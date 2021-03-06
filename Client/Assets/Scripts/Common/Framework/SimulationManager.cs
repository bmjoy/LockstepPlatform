﻿#define DEBUG_FRAME_DELAY
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lockstep.Core;
using Lockstep.Core.Logic;
using Lockstep.Logging;
using Lockstep.Serialization;
using NetMsg.Game;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Lockstep.Game {
    public class SimulationManager : SingletonManager<SimulationManager> {
        public World World => _world;
        private Contexts _context;
        private GameLog GameLog = new GameLog();
        private byte _localActorId;
        private bool Running;
        private IServiceContainer _services;
        private float _tickDt;
        private float _accumulatedTime;
        private World _world;
        private FrameBuffer cmdBuffer = new FrameBuffer();
        private int _localTick;
        private int _roomId;
        private List<long> allHashCodes = new List<long>();
        private int firstHashTick = 0;

        private byte[] _allActors;
        private int _actorCount;
        private int CurTick = 0;

        void OnEvent_OnServerFrame(object param){
            var msg = param as Msg_ServerFrames;
            cmdBuffer.PushServerFrames(msg.frames);
        }

        void OnEvent_OnServerMissFrame(object param){
            Debug.Log($"OnEvent_OnServerMissFrame");
            var msg = param as Msg_RepMissFrame;
            cmdBuffer.PushServerFrames(msg.frames,false);
            _networkService.SendMissFrameRepAck(cmdBuffer.GetMissServerFrameTick());
        }

        void OnEvent_OnRoomGameStart(object param){
            var msg = param as Msg_StartGame;
            OnGameStart(msg.RoomID, msg.SimulationSpeed, msg.ActorID, msg.AllActors);
            EventHelper.Trigger(EEvent.OnSimulationInited, null);
        }

        void OnEvent_OnAllPlayerFinishedLoad(object param){
            Debug.Log($"OnEvent_OnAllPlayerFinishedLoad");
            if (Running) return;
            _world.StartSimulate();
            Running = true;
            SetPursueTimestamp();
            EventHelper.Trigger(EEvent.OnSimulationStart, null);
        }

        public override void DoAwake(IServiceContainer services){
            _services = services;
            _context = Main.Instance.contexts;
            _inputService = _services.GetService<IInputService>();
        }

        public const int MinMissFrameReqTickDiff = 10;

        public const int MaxSimulationMsPerFrame = 20;

        private bool IsTimeout(){
            return Time.realtimeSinceStartup > _frameDeadline;
        }

        private float _frameDeadline;

        public float timestampOnPursue;
        public int tickOnPursue;

        public bool PursueServer(int minTickToBackup){
            if (_world.Tick >= cmdBuffer.curServerTick)
                return true;
            while (_world.Tick <= cmdBuffer.curServerTick) {
                var tick = _world.Tick;
                var sFrame = cmdBuffer.GetServerFrame(tick);
                if (sFrame == null)
                    return false;
                cmdBuffer.PushLocalFrame(sFrame);
                Simulate(sFrame, tick >= minTickToBackup);
                if (IsTimeout()) {
                    return false;
                }
            }

            SetPursueTimestamp();
            return true;
        }

        private void SetPursueTimestamp(){
            var ping = 35;
            var tickClientShouldPredict = 2; //(ping * 2) / NetworkDefine.UPDATE_DELTATIME + 1;
            tickOnPursue = _world.Tick + tickClientShouldPredict;
            timestampOnPursue = Time.realtimeSinceStartup;
        }

        public override void DoUpdate(float deltaTime){
            if (!Running) {
                return;
            }

            cmdBuffer.Ping = _networkService.Ping;
            cmdBuffer.UpdateFramesInfo();
            var missFrameTick = cmdBuffer.GetMissServerFrameTick();
            //客户端落后服务器太多帧 请求丢失帧
            if (cmdBuffer.IsNeedReqMissFrame()) {
                _networkService.SendMissFrameReq(missFrameTick);
            }

            //if (!cmdBuffer.CanExecuteNextFrame()) { //因为网络问题 需要等待服务器发送确认包 才能继续往前
            //    return;
            //}
            _frameDeadline = Time.realtimeSinceStartup + MaxSimulationMsPerFrame;

            var minTickToBackup = missFrameTick - FrameBuffer.SNAPSHORT_FRAME_INTERVAL;
            //追帧 无输入
            _constStateService.isPursueFrame = true;
            if (!PursueServer(minTickToBackup)) {
                _constStateService.isPursueFrame = false;
                Debug.Log($"PursueServering curTick:"  + _world.Tick);
                return;
            }
            _constStateService.isPursueFrame = false;

            var frameDeltaTime = (Time.realtimeSinceStartup - timestampOnPursue) * 1000;
            var targetTick = Mathf.CeilToInt(frameDeltaTime / NetworkDefine.UPDATE_DELTATIME) + tickOnPursue;
            //正常跑帧
            while (_world.Tick < targetTick) {
                var curTick = _world.Tick;
                cmdBuffer.UpdateFramesInfo();
                //校验服务器包  如果有预测失败 则需要进行回滚
                if (cmdBuffer.IsNeedRevert) {
                    _world.RollbackTo(cmdBuffer.nextTickToCheck,missFrameTick);
                    _world.CleanUselessSnapshot(System.Math.Min(cmdBuffer.nextTickToCheck - 1, _world.Tick));
                    minTickToBackup = System.Math.Max(minTickToBackup, _world.Tick + 1);
                    while (_world.Tick < missFrameTick) {
                        var sFrame = cmdBuffer.GetServerFrame(_world.Tick);
                        Logging.Debug.Assert(sFrame != null && sFrame.tick == _world.Tick,
                            $" logic error: server Frame  must exist tick {_world.Tick}");
                        //服务器超前 客户端 应该追上去 将服务器中的输入作为客户端输入
                        cmdBuffer.PushLocalFrame(sFrame);
                        Simulate(sFrame, _world.Tick >= minTickToBackup);
                    }

                    while (_world.Tick < curTick) {
                        var frame = cmdBuffer.GetLocalFrame(_world.Tick);
                        FillInputWithLastFrame(frame);//加上输入预判 减少回滚
                        Logging.Debug.Assert(frame != null && frame.tick == _world.Tick,
                            $" logic error: local frame must exist tick {_world.Tick}");
                        Predict(frame, _world.Tick > minTickToBackup);
                    }
                }

                {
                    if (_world.Tick == curTick) { //当前帧 没有被执行 需要执行之
                        ServerFrame cFrame = null;
                        var sFrame = cmdBuffer.GetServerFrame(_world.Tick);
                        if (sFrame != null) {
                            cFrame = sFrame;
                        }
                        else {
                            var input = new Msg_PlayerInput(curTick, _localActorId, _inputService.GetInputCmds());
                            cFrame = new ServerFrame();
                            var inputs = new Msg_PlayerInput[_actorCount];
                            inputs[_localActorId] = input;
                            cFrame.inputs = inputs;
                            cFrame.tick = curTick;
                            FillInputWithLastFrame(cFrame);
#if DEBUG_FRAME_DELAY
                            input.timeSinceStartUp = Time.realtimeSinceStartup;
#endif
                            if (curTick > cmdBuffer.maxServerTickInBuffer) { //服务器的输入还没到 需要同步输入到服务器
                                SendInput(input);
                            }
                        }
                        cmdBuffer.PushLocalFrame(cFrame);
                        Predict(cFrame);
                    }
                }
            } //end of while(_world.Tick < targetTick)
            CheckAndSendHashCodes();
        }


        private void SendInput(Msg_PlayerInput input){
            //TODO 合批次 一起发送 且连同历史未确认包一起发送
            _networkService.SendInput(input);
        }

        private void Simulate(ServerFrame frame, bool isNeedGenSnap = true){
            ProcessInputQueue(frame);
            _world.Simulate(isNeedGenSnap);
            var tick = _world.Tick;
            cmdBuffer.SetClientTick(tick);
            SetHashCode();
            if (isNeedGenSnap && tick % FrameBuffer.SNAPSHORT_FRAME_INTERVAL == 0) {
                _world.CleanUselessSnapshot(System.Math.Min(cmdBuffer.nextTickToCheck - 1, _world.Tick));
            }
        }

        private void Predict(ServerFrame frame, bool isNeedGenSnap = true){
            ProcessInputQueue(frame);
            _world.Predict(isNeedGenSnap);
            var tick = _world.Tick;
            cmdBuffer.SetClientTick(tick);
            SetHashCode();
            //清理无用 snapshot
            if (isNeedGenSnap && tick % FrameBuffer.SNAPSHORT_FRAME_INTERVAL == 0) {
                _world.CleanUselessSnapshot(System.Math.Min(cmdBuffer.nextTickToCheck - 1, _world.Tick));
            }
        }

        public override void DoDestroy(){
            Running = false;
        }

        public void OnGameStart(int roomId, int targetFps, byte localActorId, byte[] allActors,
            bool isNeedRender = true){
            Debug.Log($"hehe OnGameStart simulation");
            FrameBuffer.DebugMainActorID = localActorId;
            //初始化全局配置
            _constStateService.roomId = roomId;
            _constStateService.allActorIds = allActors;
            _constStateService.actorCount = allActors.Length;

            _localActorId = localActorId;
            _allActors = allActors;

            _localTick = 0;
            _roomId = roomId;
            GameLog.LocalActorId = localActorId;
            GameLog.AllActorIds = allActors;

            _actorCount = allActors.Length;
            _tickDt = 1000f / targetFps;
            _world = new World(_context, _timeMachineService, allActors, new GameLogicSystems(_context, _services));
        }

        private void FillInputWithLastFrame(ServerFrame frame){
            int tick = frame.tick;
            var inputs = frame.inputs;
            var lastFrameInputs = tick == 0 ? null : cmdBuffer.GetFrame(tick - 1)?.inputs;
            var curFrameInput = inputs[_localActorId];
            //将所有角色 给予默认的输入
            for (int i = 0; i < _actorCount; i++) {
                inputs[i] = new Msg_PlayerInput(tick, _allActors[i], lastFrameInputs?[i]?.Commands?.ToList());
            }

            inputs[_localActorId] = curFrameInput;
        }


        public void CheckAndSendHashCodes(){
            if (cmdBuffer.nextTickToCheck > firstHashTick) {
                var count = System.Math.Min(allHashCodes.Count, (int) (cmdBuffer.nextTickToCheck - firstHashTick));
                if (count > 0) {
                    _networkService.SendHashCodes(firstHashTick, allHashCodes, 0, count);
                    firstHashTick = firstHashTick + count;
                    allHashCodes.RemoveRange(0, count);
                }
            }
        }

        public void SetHash(int tick, long hash){
            if (tick < firstHashTick) {
                return;
            }

            var idx = (int) (tick - firstHashTick);
            if (allHashCodes.Count <= idx) {
                for (int i = 0; i < idx + 1; i++) {
                    allHashCodes.Add(0);
                }
            }

            allHashCodes[idx] = hash;
        }

        public void SetHashCode(){
            var nextTick = _world.Tick;
            var iTick = (int) nextTick - 1;
            for (int i = allHashCodes.Count; i <= iTick; i++) {
                allHashCodes.Add(0);
            }

            var hash = _world.Contexts.gameState.hashCode.value;
            allHashCodes[iTick] = _world.Contexts.gameState.hashCode.value;
            SetHash(nextTick - 1, hash);
        }


        public void DumpGameLog(Stream outputStream, bool closeStream = true){
            var serializer = new Serializer();
            serializer.Put(_context.gameState.hashCode.value);
            serializer.Put(_context.gameState.tick.value);
            outputStream.Write(serializer.Data, 0, serializer.Length);

            GameLog.WriteTo(outputStream);

            if (closeStream) {
                outputStream.Close();
            }
        }

        private void ProcessInputQueue(ServerFrame frame){
            var inputs = frame.inputs;
            foreach (var input in inputs) {
                GameLog.Add(frame.tick, input);

                foreach (var command in input.Commands) {
                    Log.Trace(this, input.ActorId + " >> " + input.Tick + ": " + input.Commands.Count());
                    var inputEntity = _context.input.CreateEntity();
                    _inputService.Execute(command, inputEntity);
                    inputEntity.AddTick(input.Tick);
                    inputEntity.AddActorId(input.ActorId);
                    inputEntity.isDestroyed = true;
                }
            }
        }
    }
}