//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public partial class SnapshotEntity {

    public Lockstep.Core.State.Snapshot.TickComponent tick { get { return (Lockstep.Core.State.Snapshot.TickComponent)GetComponent(SnapshotComponentsLookup.Tick); } }
    public bool hasTick { get { return HasComponent(SnapshotComponentsLookup.Tick); } }

    public void AddTick(uint newValue) {
        var index = SnapshotComponentsLookup.Tick;
        var component = CreateComponent<Lockstep.Core.State.Snapshot.TickComponent>(index);
        component.value = newValue;
        AddComponent(index, component);
    }

    public void ReplaceTick(uint newValue) {
        var index = SnapshotComponentsLookup.Tick;
        var component = CreateComponent<Lockstep.Core.State.Snapshot.TickComponent>(index);
        component.value = newValue;
        ReplaceComponent(index, component);
    }

    public void RemoveTick() {
        RemoveComponent(SnapshotComponentsLookup.Tick);
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
public sealed partial class SnapshotMatcher {

    static Entitas.IMatcher<SnapshotEntity> _matcherTick;

    public static Entitas.IMatcher<SnapshotEntity> Tick {
        get {
            if (_matcherTick == null) {
                var matcher = (Entitas.Matcher<SnapshotEntity>)Entitas.Matcher<SnapshotEntity>.AllOf(SnapshotComponentsLookup.Tick);
                matcher.componentNames = SnapshotComponentsLookup.componentNames;
                _matcherTick = matcher;
            }

            return _matcherTick;
        }
    }
}
