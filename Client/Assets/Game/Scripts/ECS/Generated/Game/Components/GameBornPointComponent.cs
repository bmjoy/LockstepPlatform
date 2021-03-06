//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public partial class GameEntity {

    public Lockstep.ECS.Game.BornPointComponent bornPoint { get { return (Lockstep.ECS.Game.BornPointComponent)GetComponent(GameComponentsLookup.BornPoint); } }
    public bool hasBornPoint { get { return HasComponent(GameComponentsLookup.BornPoint); } }

    public void AddBornPoint(Lockstep.Math.LVector2 newCoord) {
        var index = GameComponentsLookup.BornPoint;
        var component = CreateComponent<Lockstep.ECS.Game.BornPointComponent>(index);
        component.coord = newCoord;
        AddComponent(index, component);
    }

    public void ReplaceBornPoint(Lockstep.Math.LVector2 newCoord) {
        var index = GameComponentsLookup.BornPoint;
        var component = CreateComponent<Lockstep.ECS.Game.BornPointComponent>(index);
        component.coord = newCoord;
        ReplaceComponent(index, component);
    }

    public void RemoveBornPoint() {
        RemoveComponent(GameComponentsLookup.BornPoint);
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

    static Entitas.IMatcher<GameEntity> _matcherBornPoint;

    public static Entitas.IMatcher<GameEntity> BornPoint {
        get {
            if (_matcherBornPoint == null) {
                var matcher = (Entitas.Matcher<GameEntity>)Entitas.Matcher<GameEntity>.AllOf(GameComponentsLookup.BornPoint);
                matcher.componentNames = GameComponentsLookup.componentNames;
                _matcherBornPoint = matcher;
            }

            return _matcherBornPoint;
        }
    }
}
