using Lockstep.Serialization;

namespace Lockstep.NetMsg.Lobby {
    public partial class ServerFrames : NetMsgBase {
        public ServerFrame[] frames;

        public override void Serialize(Serializer writer){
            writer.Put(frames.Length);
            for (int i = 0; i < frames.Length; i++) {
                frames[i].Serialize(writer);
            }
        }

        public override void Deserialize(Deserializer reader){
            var len = reader.GetInt();
            frames = new ServerFrame[len];
            for (int i = 0; i < len; i++) {
                frames[i] = new ServerFrame();
                frames[i].Deserialize(reader);
            }
        }
    }
}