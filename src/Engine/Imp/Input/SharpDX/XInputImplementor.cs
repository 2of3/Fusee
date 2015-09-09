namespace Fusee.Engine
{
    // This class is instantiated dynamically (by reflection)
    public class XInputDriverImplementor
    {
        /// <summary>
        /// Creates the controller implementation.
        /// </summary>
        /// <returns>An instance of InputDriverImp is returned.</returns>
        public static IXInputDriverImp CreateXInputDriverImp()
        {
            return new XInputDriverImp();
        }
    }
}