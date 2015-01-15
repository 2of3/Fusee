using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace Fusee.Engine
{
    public class VideoFileSource
    {
		private string	_fileName;
		private Thread	_workerThread;
		private ManualResetEvent _needToStop;

		private int  _framesReceived;
        private int  _bytesReceived;
		private bool _frameIntervalFromSource;
		private int  _frameInterval;

		public event NewFrameEventHandler NewFrame;

        /// <summary>
        /// Video source error event.
        /// </summary>
        /// 
        /// <remarks>This event is used to notify clients about any type of errors occurred in
        /// video source object, for example internal exceptions.</remarks>
        ///
		public event VideoSourceErrorEventHandler VideoSourceError;

        /// <summary>
        /// Video playing finished event.
        /// </summary>
        /// 
        /// <remarks><para>This event is used to notify clients that the video playing has finished.</para>
        /// </remarks>
        /// 
		public event PlayingFinishedEventHandler PlayingFinished;

		/// <summary>
        /// Video source.
        /// </summary>
        /// 
        /// <remarks><para>Video file name to play.</para></remarks>
        /// 
		public string Source
		{
			get
			{
				return _fileName;
			}
			set
			{
				_fileName = value;
			}
		}

        /// <summary>
        /// Received frames count.
        /// </summary>
        /// 
        /// <remarks>Number of frames the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
		public int FramesReceived
		{
			get
			{
				int frames = _framesReceived;
				_framesReceived = 0;
				return frames;
			}
		}

        /// <summary>
        /// Received bytes count.
        /// </summary>
        /// 
        /// <remarks>Number of bytes the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
		public long BytesReceived
		{
			get
			{
				return 0;
			}
		}

        /// <summary>
        /// State of the video source.
        /// </summary>
        /// 
        /// <remarks>Current state of video source object - running or not.</remarks>
        /// 
		public bool IsRunning
		{
			get
			{
				if ( _workerThread != null )
				{
					// check if the thread is still running
					if ( _workerThread.Join( 0 ) == false )
						return true;

					Free( );
				}
				return false;
			}
		}

        /// <summary>
        /// Frame interval.
        /// </summary>
        /// 
        /// <remarks><para>The property sets the interval in milliseconds between frames. If the property is
        /// set to 100, then the desired frame rate will be 10 frames per second.</para>
        /// 
        /// <para><note>Setting this property to 0 leads to no delay between video frames - frames
        /// are read as fast as possible.</note></para>
		///
		/// <para><note>Setting this property has effect only when <see cref="FrameIntervalFromSource"/>
		/// is set to <see langword="false"/>.</note></para>
        /// 
        /// <para>Default value is set to <b>0</b>.</para>
        /// </remarks>
        /// 
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

        /// <summary>
        /// Get frame interval from source or use manually specified.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies which frame rate to use for video playing.
        /// If the property is set to <see langword="true"/>, then video is played
        /// with original frame rate, which is set in source video file. If the property is
        /// set to <see langword="false"/>, then custom frame rate is used, which is
        /// calculated based on the manually specified <see cref="FrameInterval">frame interval</see>.</para>
        /// 
        /// <para>Default value is set to <see langword="true"/>.</para>
        /// </remarks>
        /// 
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

        public VideoFileSource( string fileName )
        {
	        _fileName = fileName;
	        _workerThread = null;
	        _needToStop = null;

	        _frameIntervalFromSource = true;
	        _frameInterval = 0;
        }

        public void Start( )
        {
	        if ( !IsRunning )
	        {
                // check source
		        if ( string.IsNullOrEmpty( _fileName ) )
		        {
                    throw new ArgumentException( "Video file name is not specified." );
		        }

		        _framesReceived = 0;
                _bytesReceived = 0;

		        // create events
		        _needToStop = new ManualResetEvent( false );
		
		        // create and start new thread
		        _workerThread = new Thread( WorkerThreadHandler );
		        _workerThread.Name = _fileName; // just for debugging
		        _workerThread.Start( );
	        }
        }

        public void SignalToStop( )
        {
	        if ( _workerThread != null )
	        {
		        // signal to stop
		        _needToStop.Set();
	        }
        }

        public void WaitForStop( )
        {
	        if ( _workerThread != null )
	        {
		        // wait for thread stop
		        _workerThread.Join( );

		        Free( );
	        }
        }

        public void Stop( )
        {
	        if ( IsRunning )
	        {
		        _workerThread.Abort( );
		        WaitForStop( );
	        }
        }

        private void Free( )
        {
	        _workerThread = null;

	        // release events
	        _needToStop.Close( );
	        _needToStop = null;
        }

        private void WorkerThreadHandler( )
        {
	        ReasonToFinishPlaying reasonToStop = ReasonToFinishPlaying.StoppedByUser;
	        AForge.Video.FFMPEG.VideoFileReader videoReader = new AForge.Video.FFMPEG.VideoFileReader( );

	        try
	        {
		        videoReader.Open( _fileName );

                // frame interval
                int interval = ( _frameIntervalFromSource ) ?
			        (int) ( 1000 / ( ( videoReader.FrameRate == 0 ) ? 25 : videoReader.FrameRate ) ) :
			        _frameInterval;

                while ( !_needToStop.WaitOne( 0, false ) )
		        {
			        // start time
			        DateTime start = DateTime.Now;

			        // get next video frame
			        using (Bitmap bitmap = videoReader.ReadVideoFrame( ))
                    {
			            if ( bitmap == null )
			            {
				            reasonToStop = ReasonToFinishPlaying.EndOfStreamReached;
                            break;
			            }

			            _framesReceived++;
                        _bytesReceived += bitmap.Width * bitmap.Height *
                            ( Bitmap.GetPixelFormatSize( bitmap.PixelFormat ) >> 3 );

			            // notify clients about the new video frame
			            if (NewFrame != null)
                            NewFrame( this, new NewFrameEventArgs( bitmap ) );
                    }

                    // wait for a while ?
                    if ( interval > 0 )
                    {
                        // get frame extract duration
				        TimeSpan span = DateTime.Now.Subtract( start );

                        // miliseconds to sleep
                        int msec = interval - (int) span.TotalMilliseconds;

                        if ( ( msec > 0 ) && ( _needToStop.WaitOne( msec, false ) == true ) )
					        break;
                    }
		        }
	        }
	        catch ( Exception exception )
	        {
                VideoSourceError( this, new VideoSourceErrorEventArgs( exception.Message ) );
	        }

	        videoReader.Close( );
	        if (PlayingFinished != null)
                PlayingFinished( this, reasonToStop );
        }
    }
}
