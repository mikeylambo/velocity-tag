// GameEventBus.cs
// Static, strongly-typed publish/subscribe bus. Direct successor to the JS
// EventBus: game systems talk through typed events instead of hard references,
// which is what let combat.js / round.js / targets.js stay decoupled.

using System;
using System.Collections.Generic;

namespace ShooterCore
{
    public static class GameEventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public static void On<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var existing))
                _handlers[t] = (Action<T>)existing + handler;
            else
                _handlers[t] = handler;
        }

        public static void Off<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var existing))
            {
                var d = (Action<T>)existing - handler;
                if (d == null) _handlers.Remove(t);
                else _handlers[t] = d;
            }
        }

        public static void Emit<T>(T evt)
        {
            if (_handlers.TryGetValue(typeof(T), out var existing))
                ((Action<T>)existing)?.Invoke(evt);
        }

        // Call on scene teardown / domain reload safety if needed.
        public static void Clear() => _handlers.Clear();
    }
}
