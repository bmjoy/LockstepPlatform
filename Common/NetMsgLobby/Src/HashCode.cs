﻿using Lockstep.Serialization;

namespace Lockstep.NetMsg.Lobby {
    public class HashCode : ISerializable {
        public ulong FrameNumber { get; set; }
        public long Value { get; set; }

        public void Serialize(Serializer writer){
            writer.Put(FrameNumber);
            writer.Put(Value);
        }

        public void Deserialize(Deserializer reader){
            FrameNumber = reader.GetULong();
            Value = reader.GetLong();
        }
    }
}