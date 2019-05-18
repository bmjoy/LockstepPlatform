//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public partial class GameEntity {

    public Lockstep.ECS.Game.TankSkillComponent tankSkill { get { return (Lockstep.ECS.Game.TankSkillComponent)GetComponent(GameComponentsLookup.TankSkill); } }
    public bool hasTankSkill { get { return HasComponent(GameComponentsLookup.TankSkill); } }

    public void AddTankSkill(Lockstep.Math.LFloat newCd, Lockstep.Math.LFloat newTimer, int newBulletType) {
        var index = GameComponentsLookup.TankSkill;
        var component = CreateComponent<Lockstep.ECS.Game.TankSkillComponent>(index);
        component.cd = newCd;
        component.timer = newTimer;
        component.BulletType = newBulletType;
        AddComponent(index, component);
    }

    public void ReplaceTankSkill(Lockstep.Math.LFloat newCd, Lockstep.Math.LFloat newTimer, int newBulletType) {
        var index = GameComponentsLookup.TankSkill;
        var component = CreateComponent<Lockstep.ECS.Game.TankSkillComponent>(index);
        component.cd = newCd;
        component.timer = newTimer;
        component.BulletType = newBulletType;
        ReplaceComponent(index, component);
    }

    public void RemoveTankSkill() {
        RemoveComponent(GameComponentsLookup.TankSkill);
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentMatcherApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public sealed partial class GameMatcher {

    static Entitas.IMatcher<GameEntity> _matcherTankSkill;

    public static Entitas.IMatcher<GameEntity> TankSkill {
        get {
            if (_matcherTankSkill == null) {
                var matcher = (Entitas.Matcher<GameEntity>)Entitas.Matcher<GameEntity>.AllOf(GameComponentsLookup.TankSkill);
                matcher.componentNames = GameComponentsLookup.componentNames;
                _matcherTankSkill = matcher;
            }

            return _matcherTankSkill;
        }
    }
}