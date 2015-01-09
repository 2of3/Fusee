namespace Fusee.Engine
{
    /// <summary>
    /// The interface for VideoManager implementations. This interface should contain all functions
    /// to load a video.
    /// </summary>
    public interface IVideoManagerImpOld
    {
        IVideoStreamImpOld CreateVideoStreamImpFromFile(string filename, bool loopVideo, bool useAudio);
        IVideoStreamImpOld CreateVideoStreamImpFromCamera(int cameraIndex, bool useAudio);
    }
}
