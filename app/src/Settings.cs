using Godot;
using System;
using System.IO;
using System.IO.Ports;
using System.Drawing;
using System.Drawing.Imaging;
using Newtonsoft.Json;

namespace ValorantShocker
{
    public class Settings : WindowDialog
    {
        public class SettingsObject
        {
            public int x { get; set; } = 0;
            public int y { get; set; } = 0;
            public int w { get; set; } = 100;
            public int h { get; set; } = 100;
            public int confidence { get; set; } = 60;
            public int damage { get; set; } = 1;
            public int time { get; set; } = 10;
            public String port { get; set; } = "COM1";
        }

        public static SettingsObject settings = new SettingsObject();

        private SpinBox xNode { get; set; }
        private SpinBox yNode { get; set; }
        private SpinBox wNode { get; set; }
        private SpinBox hNode { get; set; }
        private SpinBox confidenceNode { get; set; }
        private SpinBox damageNode { get; set; }
        private SpinBox timeNode { get; set; }
        private LineEdit portNode { get; set; }

        public override void _Ready()
        {
            xNode = GetNode<SpinBox>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer1/x");
            yNode = GetNode<SpinBox>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer1/y");
            wNode = GetNode<SpinBox>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer1/w");
            hNode = GetNode<SpinBox>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer1/h");
            confidenceNode = GetNode<SpinBox>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer2/confidence");
            damageNode = GetNode<SpinBox>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer2/damage");
            timeNode = GetNode<SpinBox>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer2/time");
            portNode = GetNode<LineEdit>("HorizontalContainer/SettingsMargin/SettingsContainer/SettingsHContainer/SettingsVContainer2/port");

            ReadSettings(); // read settings

            OpenSerialPort(); // open Serial Port
        }

        public override void _Process(float delta)
        {
            if (Input.IsActionPressed("settings"))
            {
                Popup_();
            }
        }

        public void WriteSettings()
        {
            var json = JsonConvert.SerializeObject(settings);
            var saveFile = new Godot.File();

            // read ui values into memory
            settings.x = (int)xNode.Value;
            settings.y = (int)yNode.Value;
            settings.w = (int)wNode.Value;
            settings.h = (int)hNode.Value;
            settings.confidence = (int)confidenceNode.Value;
            settings.damage = (int)damageNode.Value;
            settings.time = (int)timeNode.Value;
            settings.port = (String)portNode.Text;

            // write setting to disk
            saveFile.Open("user://settings.json", Godot.File.ModeFlags.Write);
            saveFile.StoreLine(json);
            saveFile.Close();
            GD.Print("Saving Data: " + json);

            // update the preview in settings panel
            SetPreviewImage();

            // Restart Serial Port
            OpenSerialPort();

            // free
            saveFile.Dispose();
        }

        private void ReadSettings()
        {
            var saveFile = new Godot.File();
            var jsonString = "";

            // load settings from disk
            if (saveFile.Open("user://settings.json", Godot.File.ModeFlags.Read) == 0)
            {
                jsonString = saveFile.GetLine();
                settings = JsonConvert.DeserializeObject<SettingsObject>(jsonString);
                saveFile.Close();

                GD.Print("Read data: " + JsonConvert.SerializeObject(settings));
            }

            // update ui
            xNode.Value = (double)settings.x;
            yNode.Value = (double)settings.y;
            wNode.Value = (double)settings.w;
            hNode.Value = (double)settings.h;
            confidenceNode.Value = (double)settings.confidence;
            damageNode.Value = (double)settings.damage;
            timeNode.Value = (double)settings.time;
            portNode.Text = (String)settings.port;

            // free memory
            saveFile.Dispose();
        }

        private void SetPreviewImage()
        {
            if (Visible)
            {
                // take screenshot
                Bitmap screenshot = Main.TakeScreenshot();
                Bitmap croppedScreenshot;
                Godot.ImageTexture healthImg = new Godot.ImageTexture();

                try
                {
                    // crop screenshot
                    croppedScreenshot = Main.CropBitmap(screenshot, settings.x, settings.y, settings.w, settings.h);
                    // save sceenshot
                    croppedScreenshot.Save(OS.GetUserDataDir() + "\\" + "preview.png", ImageFormat.Png);
                    croppedScreenshot.Dispose();
                }
                catch
                {
                    GD.PrintErr("Copping failed!");
                    OS.Alert("Crop range is outside of image!");
                }

                // set screenshot as node texture
                healthImg.Load("user://preview.png");
                GetNode<TextureRect>("HorizontalContainer/PreviewMargin/PreviewImage").Texture = healthImg;

                // delete preview image
                var previewFile = new FileInfo(OS.GetUserDataDir() + "\\" + "preview.png");
                while (Main.IsFileLocked(previewFile))
                {
                    GD.Print("File in use: " + OS.GetUserDataDir() + "\\" + "preview.png");
                    System.Threading.Thread.Sleep(500);
                }
                previewFile.Delete();

                // free memory
                screenshot.Dispose();
                healthImg.Dispose();
            }
        }

        private static void OpenSerialPort()
        {
            try { Main.serialPort.Close(); }
            catch {; }

            Main.serialPort = new SerialPort(settings.port, 115200, Parity.None, 8, StopBits.One);
            Main.serialPort.Handshake = Handshake.None;
            Main.serialPort.WriteTimeout = 500;

            try { Main.serialPort.Open(); }
            catch (System.Exception ex) { OS.Alert(ex.ToString()); }
        }


    }
}
