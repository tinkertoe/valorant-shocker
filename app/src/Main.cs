using Godot;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ValorantShocker
{
	public class Main : Godot.Panel
	{
		protected static int _health = -1;

		public int Health
		{
			get { return _health; }
			set
			{
				var oldHealth = _health;
				var newHealth = value;

				// Update Health value only when confident
				if (newHealth >= 0) _health = newHealth;
				else _health = oldHealth;

				// Update UI
				var healthLabel = GetNode<Godot.Label>("VerticalContainer/InfoCContainer/InfroMContainer/InfoVContainer/HealthCContainer/Health");
				if (_health == -1) healthLabel.Text = "?";
				else healthLabel.Text = _health.ToString();

				// Check if damage was taken
				Shock(oldHealth, _health);

				// free memory
				healthLabel.Dispose();
			}
		}

		public override void _Ready()
		{
			// clean up temp files
			foreach (string sFile in System.IO.Directory.GetFiles(OS.GetUserDataDir(), "*.tsv"))
				System.IO.File.Delete(sFile);
			foreach (string sFile in System.IO.Directory.GetFiles(OS.GetUserDataDir(), "*.png"))
				System.IO.File.Delete(sFile);
		}

		public static bool IsFileLocked(FileInfo file)
		{
			FileStream stream = null;

			try
			{
				stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}

			return false;
		}

		public static Bitmap TakeScreenshot()
		{
			var screenSize = Screen.GetBounds(Point.Empty);
			var bitmap = new Bitmap(screenSize.Width, screenSize.Height);
			var g = Graphics.FromImage(bitmap);

			g.CopyFromScreen(Point.Empty, Point.Empty, screenSize.Size);
			g.Dispose();

			return bitmap;
		}

		public static Bitmap CropBitmap(Bitmap bitmap, int cropX, int cropY, int cropWidth, int cropHeight)
		{
			var rect = new Rectangle(cropX, cropY, cropWidth, cropHeight);
			var cropped = bitmap.Clone(rect, bitmap.PixelFormat);
			return cropped;
		}

		private void _ReadHealth()
		{
			var screenshot = TakeScreenshot();
			var croppedScreenshot = CropBitmap(screenshot, Settings.settings.x, Settings.settings.y, Settings.settings.w, Settings.settings.h);
			new System.Threading.Thread(() => OCR(croppedScreenshot)).Start();

			screenshot.Dispose();
		}

		private void OCR(Bitmap bitmap)
		{
			var tempImgPath = OS.GetUserDataDir() + "/" + Guid.NewGuid() + System.Threading.Thread.CurrentThread.Name + ".png";
			var outputFilePath = OS.GetUserDataDir() + "/" + Guid.NewGuid() + System.Threading.Thread.CurrentThread.Name;

			var tesseractPath = System.IO.Directory.GetCurrentDirectory() + "/tesseract";
			var flags = tempImgPath + " " + outputFilePath + " -l eng --psm 7 numbersonly";
			var process = new Process();

			var outputFile = new Godot.File();
			var output = String.Empty;
			String[] outputFormated;
			int result;
			int confidence;

			// Write image to disk
			bitmap.Save(tempImgPath, ImageFormat.Png);

			// run tesseract on temp image
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.Arguments = flags;
			process.StartInfo.FileName = tesseractPath + "/google.tesseract.tesseract-master.exe";

			process.Start();
			process.WaitForExit();


			// read tesseract output
			outputFile.Open(outputFilePath + ".tsv", Godot.File.ModeFlags.Read);
			output = outputFile.GetAsText();
			outputFile.Close();

			outputFormated = Regex.Split(output, "	");
			try
			{
				result = int.Parse(outputFormated[outputFormated.Length - 1]);
				confidence = int.Parse(outputFormated[outputFormated.Length - 2]);
			}
			catch
			{
				result = int.Parse(outputFormated[outputFormated.Length - 2]);
				confidence = int.Parse(outputFormated[outputFormated.Length - 3]);
			}

			// GD.Print("result: " + result);
			// GD.Print("confidence: " + confidence);
			// GD.Print("------");

			// set health value
			GD.Print("conf: " + confidence);
			if (confidence > Settings.settings.confidence)
			{
				Health = result;
			}
			else
			{
				Health = -1;
			}

			// delete temp files
			var file1 = new FileInfo(tempImgPath);
			while (IsFileLocked(file1))
			{
				GD.Print("File in use: " + tempImgPath);
				System.Threading.Thread.Sleep(500);
			}
			file1.Delete();

			var file2 = new FileInfo(outputFilePath + ".tsv");
			while (IsFileLocked(file2))
			{
				GD.Print("File in use: " + outputFilePath + ".tsv");
				System.Threading.Thread.Sleep(500);
			}
			file2.Delete();


			// free memory
			bitmap.Dispose();
			outputFile.Dispose();
		}

		public static SerialPort serialPort;

		private void Shock(int oldHealth, int newHealth)
		{
			// check if player took minimum amout of damage (minDmg = settings.d) and newHealth has to be confident reading
			var tookDamage = (newHealth + Settings.settings.damage <= oldHealth && newHealth != -1);
			// check if button is on
			var shockOn = GetNode<TextureButton>("VerticalContainer/ButtonMContainer/ToggleButton").Pressed;
			// convert ms to number that the arduiono can receive (0-9 / 0 = 10ms / 9 = 100ms)
			var time = (Convert.ToInt32(Settings.settings.time / 10) - 1).ToString();

			if (tookDamage && shockOn)
			{
				GetNode<Particles2D>("VerticalContainer/ButtonMContainer/ToggleButton/Particles").Emitting = true;
				if (serialPort.IsOpen == true)
					serialPort.WriteLine(time);
				else
					OS.Alert("Serial port isn't open yet");
			}
		}


	}
}
