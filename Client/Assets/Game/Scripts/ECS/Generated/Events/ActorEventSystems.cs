//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.EventSystemsGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public sealed class ActorEventSystems : Feature {

    public ActorEventSystems(Contexts contexts) {
        Add(new LifeEventSystem(contexts)); // priority: 0
        Add(new ScoreEventSystem(contexts)); // priority: 0
    }
}