using UnityEngine;

public class SpeakerStaminaComponent : StaminaComponent
{

    [Header("Speaker Attributes")]
    [SerializeField] DeflectManager deflectManager;
    [SerializeField] HealthComponent healthComponent;

    public override void InitComponent(GameManager manager)
    {
        base.InitComponent(manager);
        healthComponent.entityDamaged.AddListener(HandleDamage);
        deflectManager.deflectedBall.AddListener(HandleBallDeflect);
    }


    public override void HandleDamage(DamageInfo info)
    {
        if (info.damageSource == DamageSource.Ball)
        {
            if (InDangerZone)
            {
                foresightAuraHum.Stop();
                foresightElectricityCrackle.Stop();
                healthComponent.KillEntity(info, healthComponent); //if we're in danger and we got hit by the ball, we're KO'ed
                return;
            }
        }
        DamageStamina(info.damage, info.maxStaminaDamage, info.dealsGrayStaminaDamage);

    }

    public void HandleBallDeflect(BaseEcho ball, bool partialDeflect, bool usedSkill)
    {
        if (!partialDeflect)
        {
            if (GrayStamina > 0.0f) { regainedGrayStamina.Invoke(characterOwner); }
            Stamina += GrayStamina; // since we had gray while we deflected, we convert gray stamina to usable stamina
            GrayStamina = 0; // then clear it 
            Stamina = Mathf.Clamp(Stamina, 1, MaxStamina);
            if (!usedSkill) EnableForesight();
        }
        else
        {
            DamageStamina(PARTIAL_DEFLECT_STAMINA_DAMAGE, 0, true);
        }

    }
}