using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public struct NetworkTimer : INetworkStruct
    {
        private float _targetTick;
        private float _initialTick;

        public bool Expired(NetworkRunner runner) => runner.IsRunning && (Tick) _targetTick <= runner.SimulationTime;

        public bool IsRunning => _targetTick > 0;

        public static NetworkTimer CreateFromTicks(NetworkRunner runner, int timerTickLength)
        {
            if (runner == false || runner.IsRunning == false)
                return new NetworkTimer();

            NetworkTimer fromTicks = new NetworkTimer
            {
                _targetTick = (int) runner.SimulationTime + timerTickLength,
                _initialTick = runner.SimulationTime
            };
            
            return fromTicks;
        }

        public float NormalizedValue(NetworkRunner runner)
        {
            if (runner == null || runner.IsRunning == false || IsRunning == false)
                return 0;

            if (Expired(runner))
                return 1;

            return ElapsedTicks(runner) / (_targetTick - (float)_initialTick);
        }

        public float ElapsedTicks(NetworkRunner runner)
        {
            if (runner == false || runner.IsRunning == false)
                return 0;

            if (IsRunning == false || Expired(runner))
                return 0;

            return runner.SimulationTime - _initialTick;
        }
    }

}

