namespace Fusee.Engine
{
    // This class is instantiated dynamically (by reflection)
    /// <summary>
    /// VideoManager Implementor class. This class is called by ImpFactory. Do not use this.
    /// </summary>
    public class VideoManagerImplementor
    {
        /// <summary>
        /// Creates the VideoManager implementation.
        /// </summary>
        /// <returns>An instance of VideoManagerImpOld.</returns>
        public static IVideoManagerImpOld CreateVideoManagerImp()
        {
            return new VideoManagerImpOld();
        }
    }
}
