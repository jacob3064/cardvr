﻿using System;
using System.Collections.Generic;
using System.Text;
using AForge.Video.DirectShow;
using System.Drawing;
using AForge.Video;

namespace CarDVR
{
	class VideoManager
	{
		private static readonly Font framefont = new Font("Arial", 8, FontStyle.Bold);
		private static readonly Point pointWhite = new Point(5, 5);
		private static readonly Point pointBlack = new Point(6, 6);

		// TODO: make stand alone class FramesCounter
		int lastFrames = 0, totalFrames = 0, lastFps = 0;
		object framesCountKeeper = new object();

		VideoCaptureDevice webcam = null;
		Bitmap frame;
		public object frameKeeper = new object();

		VideoSplitter splitter = new VideoSplitter();

		System.Timers.Timer FpsDisplayer = new System.Timers.Timer();
		System.Timers.Timer timerWriter = new System.Timers.Timer();

		GpsReceiver gps;

		public NewFrameEventHandler NewFrame;


		public VideoManager(GpsReceiver gpsRcvr)
		{
			gps = gpsRcvr;

			FpsDisplayer.Interval = 1000;
			FpsDisplayer.Elapsed += new System.Timers.ElapsedEventHandler(FpsDisplayer_Tick);
			FpsDisplayer.Enabled = false;

			timerWriter.Interval = 40;
			timerWriter.Elapsed += new System.Timers.ElapsedEventHandler(timerWriter_Tick);
			timerWriter.Enabled = true;
		}


		public bool IsRecording()
		{
			return webcam != null && webcam.IsRunning;
		}

		public void Initialize()
		{
			// locking frameKeeper to prevent using video source
			lock (frameKeeper)
			{
				if (webcam != null)
					webcam.NewFrame -= videoSource_NewFrame;

				webcam = new VideoCaptureDevice(Program.settings.VideoSourceId);
				webcam.NewFrame += videoSource_NewFrame;
				webcam.DesiredFrameRate = Program.settings.VideoFps;
				webcam.DesiredFrameSize = new Size(Program.settings.VideoWidth, Program.settings.VideoHeight);

				splitter.Codec = Program.settings.Codec;
				splitter.FPS = Program.settings.OutputRateFps != 0 ? Program.settings.OutputRateFps : Program.settings.VideoFps;
				splitter.VideoSize = Program.settings.GetVideoSize();
				splitter.FileDuration = Program.settings.AviDuration;
				splitter.NumberOfFiles = Program.settings.AmountOfFiles;
				splitter.Path = Program.settings.PathForVideo;

				if (splitter.FPS > 0)
					timerWriter.Interval = 1000 / splitter.FPS;
			}
		}

		void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
		{
			++totalFrames;

			lock (frameKeeper)
			{
				frame = eventArgs.Frame; // (Bitmap)eventArgs.Frame.Clone();

				if (Program.settings.EnableRotate)
				{
					switch (Program.settings.RotateAngle)
					{
						case 90:
							frame.RotateFlip(RotateFlipType.Rotate90FlipNone);
							break;
						case 180:
							frame.RotateFlip(RotateFlipType.Rotate180FlipNone);
							break;
						case 270:
							frame.RotateFlip(RotateFlipType.Rotate270FlipNone);
							break;
					}
				}

				using (Graphics graphics = Graphics.FromImage(frame))
				{
					string frameString = MakeFrameString();
					graphics.DrawString(frameString, framefont, Brushes.Black, pointBlack);
					graphics.DrawString(frameString, framefont, Brushes.White, pointWhite);
				}

				if (NewFrame != null)
					NewFrame(sender, new NewFrameEventArgs(frame)); //(Bitmap)frame.Clone()));
			}
		}

		public void Start()
		{
			splitter.Start();
			webcam.Start();

			FpsDisplayer.Enabled = true;
		}

		public void Stop()
		{
			FpsDisplayer.Enabled = false;

			webcam.Stop();
			webcam.WaitForStop();
			splitter.Stop();
		}

		private string MakeFrameString()
		{
			string result = DateTime.Now.ToString() + " ";

			if (Program.settings.GpsEnabled)
			{
				switch (gps.State)
				{
					case GpsState.Active:
						result += MainForm.resSpeed + " " + gps.Speed + " " +
									MainForm.resKmh + " " +
									MainForm.resSatellites + " " + gps.NumberOfSatellites.ToString() + "\n" + gps.Coordinates;
						break;
					case GpsState.NoSignal:
						result += MainForm.resNoGpsSignal;
						break;
					case GpsState.NotActive:
						result += MainForm.resGpsNotConnected;
						break;
				}
			}

			lock (framesCountKeeper)
			{
				result += "\n" + totalFrames.ToString() + " | " + lastFps.ToString() + " FPS";
			}

			return result;
		}

		private void FpsDisplayer_Tick(object sender, EventArgs e)
		{
			lock (framesCountKeeper)
			{
				lastFps = totalFrames - lastFrames;
				lastFrames = totalFrames;
			}
		}

		private void timerWriter_Tick(object sender, EventArgs e)
		{
			lock (frameKeeper)
			{
				splitter.AddFrame(ref frame);

				//if (Visible)
				//    camView.Image = frame;
			}
		}

	}
}