using Gma.System.MouseKeyHook;
using System;

namespace GameAiAndControls.Input
{
    public static class InputManager
    {
        private static IKeyboardMouseEvents _sharedHook;

        /// <summary>
        /// Gets the shared global keyboard/mouse hook.
        /// Initializes it if not already set.
        /// </summary>
        public static IKeyboardMouseEvents SharedHook
        {
            get
            {
                if (_sharedHook == null)
                {
                    Console.WriteLine("InputManager: Initializing global hook.");
                    _sharedHook = Hook.GlobalEvents();
                }
                return _sharedHook;
            }
        }

        /// <summary>
        /// Optional: call this on application shutdown if you want to clean up the global hook.
        /// Do not call this mid-game unless you are restarting the entire input system.
        /// </summary>
        public static void Shutdown()
        {
            if (_sharedHook != null)
            {
                Console.WriteLine("InputManager: Disposing global hook.");
                _sharedHook.Dispose();
                _sharedHook = null;
            }
        }
    }
}