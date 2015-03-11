using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
// using AForge.Video.FFMPEG;
// using AForge.Video.DirectShow;

namespace Fusee.Engine
{

    /// <summary>
    /// This class provides all fuctions to control the video playback and to obtain single images from the stream.
    /// </summary>
    public class VideoStreamImp : IVideoStreamImp
    {
        private ImageData _nextFrame;

        private string _fileName;
        private Thread _workerThread;
        private ManualResetEvent _needToStop;

        private int _framesReceived;
        private int _bytesReceived;
        private bool _frameIntervalFromSource;
        private int _frameInterval;

        public event NewFrameEventHandler NewFrame;


        public int FramesReceived
        {
            get
            {
                int frames = _framesReceived;
                _framesReceived = 0;
                return frames;
            }
        }

        public bool IsRunning
        {
            get
            {
                if (_workerThread != null)
                {
                    // check if the thread is still running
                    if (_workerThread.Join(0) == false)
                        return true;

                    Free();
                }
                return false;
            }
        }

        public int FrameInterval
        {
            get
            {
                return _frameInterval;
            }
            set
            {
                _frameInterval = value;
            }
        }


        public bool FrameIntervalFromSource
        {
            get
            {
                return _frameIntervalFromSource;
            }
            set
            {
                _frameIntervalFromSource = value;
            }
        }


        #region Constructors
        // TODO: integrate VideoFileSource. Take the file name instead of the source
        public VideoStreamImp(VideoFileSource source, bool loopVideo, bool useAudio)
        {
            _source = source;
            _source.NewFrame += NextFrame;
            if (loopVideo)
                _source.PlayingFinished += PlayingFinished;
            _source.VideoSourceError += VideoSourceError;
            _source.WaitForStop();
            _source.Start();
        }
        #endregion

        #region Events

        /// <summary>
        /// This event is called every time a new frame is available.
        /// In this event the ImageData struct is updated with the PixelData from the current frame.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void NextFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap nextFrameBmp = (Bitmap)eventArgs.Frame;
            nextFrameBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData bmpData = nextFrameBmp.LockBits(new System.Drawing.Rectangle(0, 0, nextFrameBmp.Width, nextFrameBmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int strideAbs = (bmpData.Stride < 0) ? -bmpData.Stride : bmpData.Stride;
            int bytes = (strideAbs) * nextFrameBmp.Height;

            _nextFrame = new ImageData
            {
                PixelData = new byte[bytes],
                Height = bmpData.Height,
                Width = bmpData.Width,
                PixelFormat = ImagePixelFormat.RGB,
                Stride = bmpData.Stride
            };
            
            Marshal.Copy(bmpData.Scan0, _nextFrame.PixelData, 0, bytes);
            nextFrameBmp.UnlockBits(bmpData);
        }

        /// <summary>
        /// This event is called when the stream finishes and loopAudio is set to true.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reason"></param>
        public void PlayingFinished (object sender, ReasonToFinishPlaying reason)
        {  
            _source = new VideoFileSource(_source.Source);
            _source.NewFrame += NextFrame;
            _source.PlayingFinished += PlayingFinished;
            _source.Start();

        }

        /// <summary>
        /// This is event is called if an error with ther playback occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void VideoSourceError(object sender, VideoSourceErrorEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(args.Description);
        }
        #endregion

        #region Members

        /// <summary>
        /// Gets the current video frame.
        /// </summary>
        /// <returns>An ImageData-struct containing the current video frame.</returns>
        public ImageData GetCurrentFrame()
        {
            return _nextFrame;
        }

        public float Volume
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public bool Loop
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public float Panning
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public void Play(bool loop)
        {
            throw new System.NotImplementedException();
        }

        public void Pause()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Stops the video playback.
        /// </summary>
        public void Stop()
        {
            _source.SignalToStop();
        }

        /// <summary>
        /// Starts the video playback.
        /// </summary>
        public void Play()
        {
            _source.Start();
        }
        #endregion
    }





    /////////////////////// ORIG
    /// <summary>
    /// This class provides all fuctions to control the video playback and to obtain single images from the stream.
    /// </summary>
    public class VideoStreamImpOrig : IVideoStreamImp
    {
        #region Fields
        private ImageData _nextFrame;
        private VideoFileSource _source;
        // private VideoCaptureDevice _videoCaptureDevice;
        #endregion

        #region Constructors
        public VideoStreamImpOrig(VideoFileSource source, bool loopVideo, bool useAudio)
        {
            _source = source;
            _source.NewFrame += NextFrame;
            if (loopVideo)
                _source.PlayingFinished += PlayingFinished;
            _source.VideoSourceError += VideoSourceError;
            _source.WaitForStop();
            _source.Start();
        }

        /*
        public VideoStreamImp (VideoCaptureDevice videoCaptureDevice, bool useAudio)
        {
            _videoCaptureDevice = videoCaptureDevice;
            _videoCaptureDevice.NewFrame += NextFrame;
            _videoCaptureDevice.VideoSourceError += VideoSourceError;
            _videoCaptureDevice.Start();
        }
        */
        #endregion

        #region Events

        /// <summary>
        /// This event is called every time a new frame is available.
        /// In this event the ImageData struct is updated with the PixelData from the current frame.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void NextFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap nextFrameBmp = (Bitmap)eventArgs.Frame;
            nextFrameBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData bmpData = nextFrameBmp.LockBits(new System.Drawing.Rectangle(0, 0, nextFrameBmp.Width, nextFrameBmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int strideAbs = (bmpData.Stride < 0) ? -bmpData.Stride : bmpData.Stride;
            int bytes = (strideAbs) * nextFrameBmp.Height;

            _nextFrame = new ImageData
            {
                PixelData = new byte[bytes],
                Height = bmpData.Height,
                Width = bmpData.Width,
                PixelFormat = ImagePixelFormat.RGB,
                Stride = bmpData.Stride
            };

            Marshal.Copy(bmpData.Scan0, _nextFrame.PixelData, 0, bytes);
            nextFrameBmp.UnlockBits(bmpData);
        }

        /// <summary>
        /// This event is called when the stream finishes and loopAudio is set to true.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reason"></param>
        public void PlayingFinished(object sender, ReasonToFinishPlaying reason)
        {
            _source = new VideoFileSource(_source.Source);
            _source.NewFrame += NextFrame;
            _source.PlayingFinished += PlayingFinished;
            _source.Start();

        }

        /// <summary>
        /// This is event is called if an error with ther playback occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void VideoSourceError(object sender, VideoSourceErrorEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(args.Description);
        }
        #endregion

        #region Members

        /// <summary>
        /// Gets the current video frame.
        /// </summary>
        /// <returns>An ImageData-struct containing the current video frame.</returns>
        public ImageData GetCurrentFrame()
        {
            return _nextFrame;
        }

        public float Volume
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public bool Loop
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public float Panning
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public void Play(bool loop)
        {
            throw new System.NotImplementedException();
        }

        public void Pause()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Stops the video playback.
        /// </summary>
        public void Stop()
        {
            _source.SignalToStop();
        }

        /// <summary>
        /// Starts the video playback.
        /// </summary>
        public void Play()
        {
            _source.Start();
        }
        #endregion
    }
}

