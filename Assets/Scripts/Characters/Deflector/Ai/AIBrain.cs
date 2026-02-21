using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AIBrain : MonoBehaviour
{

    const float RISK_FACTOR_CHANGE_IN_PERCENT_TO_ALTER_BEHAVIOR = 150; //must be x more or less dangerous to consider changed
    const float MAX_DISTANCE_TO_CONSIDER_DANGER = 150.0f;
    const float MAX_DISTANCE_TO_CONSIDER_DEFLECT = 30.0f;
    const float MIN_SPEAKER_RISK_LEVEL = 25.0f; //unlike echoes, speakers are always dangerous
    const float ECHO_IGNITION_RISK_INFLUENCE = 1.7f;
    const int MAX_FRAMES_BEFORE_DIRECTION_CHANGE = 25;
    const float DANGER_ZONE_AGGRESSION_INFLUENCE = 0.4f;
    const float OPPONENT_DANGER_ZONE_AGGRESSION_INFLUENCE = 1.65f;
    const float STAMINA_WEIGHT_RANGE = 1.25f;
    const float MAX_AGGRESSION_CHANGE = 1.5f;
    const float MIN_RISK_LEVEL = 100;
    const float MAX_RISK_LEVEL = 3000;
    const float MIN_RANGE_FROM_ECHO_FOR_DEFLECTION_CHANCE = 3f;
    const float MAX_RANGE_FROM_ECHO_FOR_DEFLECTION_CHANCE = 250f;
    const int MAX_DEFLECT_CHANCE_IF_ECHO_MISS_PREDICTED_MODIFIER = 15;
    const int MAX_ATTEMPTS = 50;


    [System.Serializable]
    public class AIPersonality
    {
        public float aggressionLevel = 0.5f; //AI perferred range
        public int deflectionChance = 85; //time before AI responds
        public int reactionVariance = 8 ;
        public float riskTolerance = 1.0f; //how aggressively AI reacts to external simuli


        [HideInInspector] public float functionalAggression = 0;
        [HideInInspector] public int functionalDeflectionChance = 0;


        [HideInInspector] public float aggressionLimiter = 0;
        public AIPersonality()
        {
            functionalAggression = aggressionLevel;
            functionalDeflectionChance = deflectionChance;

            aggressionLimiter = (riskTolerance * MAX_AGGRESSION_CHANGE) * aggressionLevel;
        }
    }

    [SerializeField] AIPersonality currentPersonality = new();
    [SerializeField] VelocityManager velocityManager;
    [SerializeField] BaseSpeaker character;
    [SerializeField] BufferHelper deflectBuffer;

    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask terrainMask;
    [SerializeField] LayerMask speakerMask;
    [SerializeField] Collider _rbCollider;

    BaseSpeaker enemySpeaker;
    BaseEcho enemyEcho;



    int timeUntilDirectionChange = 8;

    Vector3 movementDir = new();

    enum MovementRelativeToEnemyResult
    {
        Engaging,
        Disengaging,
        Maintaining
    }

    public void InitBrain()
    {
        StartCoroutine(FindOppositeSpeaker());
        StartCoroutine(FindOppositeEcho());
    }

    IEnumerator FindOppositeSpeaker()
    {
        yield return new WaitForFixedUpdate();
        var speakers = FindObjectsByType<BaseSpeaker>(FindObjectsSortMode.None);
        foreach (var speaker in speakers)
        {
            if (speaker.teamIndex == character.teamIndex) { continue; }
            Debug.Log(character.name + " is looking at char " + speaker.name);
            enemySpeaker = speaker;
            break;
        }
    }

    IEnumerator FindOppositeEcho()
    {
        yield return new WaitForFixedUpdate();
        var echoes = FindObjectsByType<BaseEcho>(FindObjectsSortMode.None);
        if (echoes.Length == 1)
        {
            enemyEcho = echoes[0];
        }
        else
        {
            foreach (var echo in echoes)
            {
                if (echo.teamIndex == character.teamIndex) { continue; }
                Debug.Log(character.name + " is looking at char " + echo.name);
                enemyEcho = echo;
                break;
            }
        }
    }


    public void PhysicsUpdate()
    {

        if (enemySpeaker == null || enemyEcho == null)
        {
            if (enemySpeaker == null) Debug.Log("Could not find enemy speaker");
            if (enemyEcho == null) Debug.Log("Could not find enemy echo");
            return;
        }
        CalculateFunctionalStats();
        timeUntilDirectionChange--;
        if (timeUntilDirectionChange <= 0)
        {
            timeUntilDirectionChange = Random.Range(1, MAX_FRAMES_BEFORE_DIRECTION_CHANGE + 1);
            CalculateMovementDirection();
        }
        DeflectionLogic();
    }

    private void DeflectionLogic()
    {
        if (character.deflectManager.IsDeflecting || enemyEcho.GetTarget() != character.transform) return;
        var distanceFromEcho = Vector3.Distance(character.transform.position, enemyEcho.transform.position);
        if (distanceFromEcho > MAX_DISTANCE_TO_CONSIDER_DEFLECT) return;



        

        var range = MAX_RANGE_FROM_ECHO_FOR_DEFLECTION_CHANCE - MIN_RANGE_FROM_ECHO_FOR_DEFLECTION_CHANCE;

        var distanceAsPercent = distanceFromEcho / range;

        var reactionModifier = Mathf.Lerp(-currentPersonality.reactionVariance, currentPersonality.reactionVariance, 1 - distanceAsPercent);
        if (!EchoHitPredicted()) reactionModifier /= MAX_DEFLECT_CHANCE_IF_ECHO_MISS_PREDICTED_MODIFIER;

        var finalReactionValue = currentPersonality.functionalDeflectionChance + reactionModifier;
        var chance = Random.Range(0, 101);

        Debug.Log("reaction value is " + finalReactionValue);
        Debug.Log("reaction value needed is " + chance);
        if (chance < finalReactionValue)
        {
            deflectBuffer.BufferInput("Deflect");
        }
    }

    bool EchoHitPredicted()
    {
        Ray ray = new (enemyEcho.transform.position, enemyEcho.velocityManager.GetTotalSpeed().normalized);

        return Physics.Raycast(ray, 150, speakerMask, QueryTriggerInteraction.Collide);
    }
    private void CalculateFunctionalStats()
    {
        CalculateFunctionalAggression();
        CalculateFunctionalReactionTime();
    }

    private void CalculateFunctionalAggression()
    {

        float currentStamina = character.staminaComponent.Stamina / 100;
        float enemyStamina = enemySpeaker.staminaComponent.Stamina / 100;

        float staminaWeight = ConvertRange(-1.0f, 1.0f, 0, STAMINA_WEIGHT_RANGE, currentStamina - enemyStamina);

        if (character.staminaComponent.InDangerZone) staminaWeight *= DANGER_ZONE_AGGRESSION_INFLUENCE; //be less aggressive while in danger
        if (enemySpeaker.staminaComponent.InDangerZone) staminaWeight *= OPPONENT_DANGER_ZONE_AGGRESSION_INFLUENCE; //be more aggressive while opponent in danger

        currentPersonality.functionalAggression = currentPersonality.aggressionLevel * staminaWeight;

        currentPersonality.functionalAggression = Mathf.Clamp(currentPersonality.functionalAggression, 0.0f, currentPersonality.aggressionLimiter);
        
    }

    private void CalculateFunctionalReactionTime()
    {
       int reactionVariance = Random.Range(-currentPersonality.reactionVariance, currentPersonality.reactionVariance);
        currentPersonality.functionalDeflectionChance = currentPersonality.deflectionChance + reactionVariance;
    }
    public Vector3 GetMovementDirection()
    {
        return movementDir;
    }

    bool SimulatedMovementWillBeGrounded(Vector3 translation)
    {
        float castDistance = (_rbCollider.bounds.size.y / 2.0f) + BaseState.SAFE_MARGIN;
        bool hit = Physics.BoxCast
            (
            _rbCollider.bounds.center + translation,
            _rbCollider.bounds.size / 2.0f * BaseState.BOXCAST_RATIO,
            Vector3.down,
            character.transform.rotation,
            castDistance,
            groundMask
            );

        return hit;

        //float castDistance = (_rbCollider.bounds.size.y / 2.0f) + SAFE_MARGIN;
        //bool hit = Physics.BoxCast
        //    (
        //    _rbCollider.bounds.center,
        //    _rbCollider.bounds.size / 2.0f * BOXCAST_RATIO,
        //    Vector3.down,
        //    character.transform.rotation,
        //    castDistance,
        //    groundMask
        //    );
        //return hit;
    }

    bool SimulatedMovementWillTouchWall(Vector3 pos)
    {
        return Physics.Raycast(pos, velocityManager.GetTotalSpeed().normalized, (_rbCollider.bounds.size.magnitude / 2.0f) + BaseState.SAFE_MARGIN, terrainMask, QueryTriggerInteraction.Ignore);
    }

    float CalculateRiskFactors(Vector3 pos)
    {
        float totalRiskFactor = 0;
        float echoRisk = CalculateEchoRisk(pos);
        float speakerRisk = CalculateSpeakerRisk(pos);
        totalRiskFactor += echoRisk;
        totalRiskFactor += speakerRisk;
        totalRiskFactor *= CalculateResourceRisk();
        return totalRiskFactor;
    }

    float CalculateEchoRisk(Vector3 pos)
    {
        float distanceToEcho = Vector3.Distance(enemyEcho.transform.position, pos);
        float distanceRiskLevel;
        if (distanceToEcho > MAX_DISTANCE_TO_CONSIDER_DANGER) distanceRiskLevel = 0.0f;
        else distanceRiskLevel = MAX_DISTANCE_TO_CONSIDER_DANGER / distanceToEcho;
        bool strikableByEcho = enemyEcho.GetTarget() == character;
        bool echoIgnited = enemyEcho.isIgnited;
        float echoRiskFactor = distanceRiskLevel;
        if (strikableByEcho) echoRiskFactor *= 2;
        if (echoIgnited) echoRiskFactor *= ECHO_IGNITION_RISK_INFLUENCE;

        bool deflecting = character.deflectManager.IsDeflecting;
        if (deflecting && !character.deflectManager.IsPartialDeflect()) echoRiskFactor = 0; //echo is no threat while im deflecting, more likely to be walking forward while deflecting.
        return echoRiskFactor;
    }

    float CalculateSpeakerRisk(Vector3 pos)
    {
        float distanceToSpeaker = Vector3.Distance(enemySpeaker.transform.position, pos);
        float distanceRiskLevel = MIN_SPEAKER_RISK_LEVEL;
        if (distanceToSpeaker <= MAX_DISTANCE_TO_CONSIDER_DANGER) distanceRiskLevel = MAX_DISTANCE_TO_CONSIDER_DANGER / distanceToSpeaker;
        float staminaRiskLevel = enemySpeaker.staminaComponent.Stamina;
        if (enemySpeaker.staminaComponent.ForesightEnabled) staminaRiskLevel += MIN_SPEAKER_RISK_LEVEL; //they are able to use at least 1 more skill than usual

        return distanceRiskLevel * staminaRiskLevel;
    }

    float CalculateResourceRisk()
    {
        float staminaRisk = 100 - character.staminaComponent.Stamina;
        if (character.staminaComponent.InDangerZone) staminaRisk *= 2;

        return staminaRisk;
    }

    float CalculateDesiredRiskLevel()
    {
        return Mathf.Lerp(MIN_RISK_LEVEL, MAX_RISK_LEVEL, currentPersonality.functionalAggression / currentPersonality.aggressionLimiter);
    }
    public void CalculateMovementDirection()
    {
        bool acceptableDirection = false;
        Vector3 currentSpeed = velocityManager.GetTotalSpeed(); // assume all speed is manuplatable for simplicity
        float targetRiskLevel = CalculateDesiredRiskLevel();
        float currentRiskLevel = CalculateRiskFactors(character.transform.position);
        MovementRelativeToEnemyResult desiredMovementResult = GenerateMovementResult(targetRiskLevel, currentRiskLevel);
        int attempts = 0;
        while (!acceptableDirection)
        {
            float x = Random.Range(-1.0f, 1.0001f);
            float z = Random.Range(-1.0f, 1.0001f);

            if (movementDir.sqrMagnitude < 0.001f)
            {
                movementDir = new Vector3(x, 0, z);
            }
            else
            {
                movementDir = new Vector3(x, 0, z).normalized;
            }

            Vector3 movement = Mathf.Max(currentSpeed.magnitude, 0.01f) * movementDir;
            Vector3 simulatedPos = character.transform.position + movement;
            if (!SimulatedMovementWillBeGrounded(movement) && currentSpeed.y <= 0)
            {
                Debug.Log("Wont be grounded, moving on");
                attempts++;
                if (attempts > MAX_ATTEMPTS) break;
                continue;
            }
            else if (SimulatedMovementWillTouchWall(simulatedPos))
            {
                Debug.Log("Will be touching wall, moving on");
                attempts++;
                if (attempts > MAX_ATTEMPTS) break;
                continue;
            }

            float newRiskLevel = CalculateRiskFactors(simulatedPos);

            MovementRelativeToEnemyResult newMovementResult = GenerateMovementResult(targetRiskLevel, newRiskLevel);

            if (desiredMovementResult == newMovementResult) acceptableDirection = true;
            attempts++;
            if (attempts > MAX_ATTEMPTS) break;

            Debug.Log("On Attempt " + attempts + ", generated vector " + movementDir);
        }
    }

    MovementRelativeToEnemyResult GenerateMovementResult(float targetRisk, float alternateRisk)
    {
        if (Mathf.Abs(alternateRisk - targetRisk) < RISK_FACTOR_CHANGE_IN_PERCENT_TO_ALTER_BEHAVIOR) return MovementRelativeToEnemyResult.Maintaining;
        else if (alternateRisk < targetRisk) return MovementRelativeToEnemyResult.Engaging;
        else  return MovementRelativeToEnemyResult.Disengaging;
    }


    // Source - https://stackoverflow.com/a
    // Posted by Wim Coenen, modified by community. See post 'Timeline' for change history
    // Retrieved 2025-12-28, License - CC BY-SA 2.5

    float ConvertRange(
        float originalStart, float originalEnd, // original range
        float newStart, float newEnd, // desired range
        float value) // value to convert
    {
        float scale = (newEnd - newStart) / (originalEnd - originalStart);
        return newStart + ((value - originalStart) * scale);
    }

}
