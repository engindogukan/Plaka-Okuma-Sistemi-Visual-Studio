using openalprnet;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using plateuygulama;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using System.Threading;
using System.Timers;
using System.Drawing;
using System.Drawing.Printing;

namespace PlateRecognitionALPR
{
    public partial class Form1 : Form
    {

        private VideoCapture capture = new VideoCapture(0);
        private string MediaFile;
        static String config_file = Path.Combine(AssemblyDirectory, "openalpr.conf");
        static String runtime_data_dir = Path.Combine(AssemblyDirectory, "runtime_data");
        AlprNet alpr = new AlprNet("eu", config_file, runtime_data_dir);

        dosmanveritabaniEntities1 db = new dosmanveritabaniEntities1();
        plates pl = new plates();


        private Font printFont;
        private StreamReader streamToPrint;
        static string filePath;
        string fisyazi = "";


        public Form1()
        {

            InitializeComponent();
            Run();
     


        }

        private void Run()
        {
          
            Application.Idle += ProcessFrame;
            Application.Idle += resimisle;
        }
      


        public static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public Rectangle boundingRectangle(List<Point> points)
        {
            var minX = points.Min(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxX = points.Max(p => p.X);
            var maxY = points.Max(p => p.Y);

            return new Rectangle(new Point(minX, minY), new Size(maxX - minX, maxY - minY));
        }

        //   private static Image<Bgr,Byte> cropImage(Image img, Rectangle cropArea)
        // {
        //   var bmpImage = new Bitmap(img);
        // Image<Bgr,Byte> im = new Image<Bgr, Byte>(bmpImage.Clone(cropArea, bmpImage.PixelFormat));
        //return im;
        //}

      /*  public static Bitmap combineImages(List<Image> images)
        {
            Bitmap finalImage = null;

            try
            {
                var width = 0;
                var height = 0;

                foreach (var bmp in images)
                {
                    width += bmp.Width;
                    height = bmp.Height > height ? bmp.Height : height;
                }
                finalImage = new Bitmap(width, height);
                using (var g = Graphics.FromImage(finalImage))
                {
                    g.Clear(Color.Black);
                    var offset = 0;
                    foreach (Bitmap image in images)
                    {
                        g.DrawImage(image,
                                    new Rectangle(offset, 0, image.Width, image.Height));
                        offset += image.Width;
                    }
                }

                return finalImage;
            }
            catch (Exception ex)
            {
                if (finalImage != null)
                    finalImage.Dispose();

                throw ex;
            }
            finally
            {
                foreach (var image in images)
                {
                    image.Dispose();
                }
            }
        }*/

        string fileName;
        int n = 0;

        private void processImageFile(Emgu.CV.Image<Bgr, Byte> framepic)
        {


          //  var region = checkAmerika.Checked ? "us" : "eu";

            var alpr = new AlprNet("eu", config_file, runtime_data_dir);

            if (!alpr.IsLoaded())
            {
                txtPlaka.Text = "Error initializing OpenALPR";
                return;
            }

            //     Image<Bgr, byte> resultx = null;
            //  resultx = framepic.Copy();

            var results = alpr.Recognize(framepic.Bitmap);
            bool bulundu = false;
            var images = new List<Image>(results.Plates.Count());
            var i = 1;
          
            foreach (var result in results.Plates)
            {

                var rect = boundingRectangle(result.PlatePoints);
                Image<Bgr, Byte> img = framepic;
                //     Image<Bgr, Byte> cropped = cropImage(img.Bitmap, rect);
                //       images.Add(cropped.Bitmap);

                // Create a red rectangle around the plate if it is Turkish

                Point p = new Point(result.PlatePoints[0].X - 4, result.PlatePoints[0].Y - 25);
                //      p.Offset(0, cropped.Size.Height);
                // resultx.Draw(new Rectangle(p, cropped.Size), new Bgr(12, 12, 214), 3);
                // resultx.ROI = new Rectangle(p, cropped.Size);
                try
                {
                    //   cropped.CopyTo(resultx);
                }
                catch (Exception exx)
                {
                    continue;
                }


                //   resultx.ROI = Rectangle.Empty;

                String t = GetMatchedPlate(result.TopNPlates);
                //   picOriginResim.Image = smoothedGrayFrame;
                Regex regex = new Regex(@"^(0[0-9]|[1-7][0-9]|8[01])(([A-Z])(\d{4,5})|([A-Z]{2})(\d{3,4})|([A-Z]{3})(\d{2,3}))$");

                Match match = regex.Match(t.Replace(" ", ""));
                if (match.Success)
                {
                    DateTime aDate = DateTime.Now;
                    txtPlaka.Text = t;
                    //   picOriginResim.Image = smoothedGrayFrame;
                    sqlPlateEkle(t);



                    do
                    {
                        fileName = DateTime.Now+ " plaka" +t+ ".jpg";
                    } while (System.IO.File.Exists(fileName));


                    img.Save(@"D:\Plaka Sistemi\Plaka Resimleri" + fileName);

                    bulundu = true;
                }


            }

        /*    if (images.Any())
            {
             //   picPlakaResmi.Image = combineImages(images);
            }

            if (!bulundu)
            {
           //      picOriginResim.Image = framepic;


            }*/
            //}
        }

        private string GetMatchedPlate(List<AlprPlateNet> plakalar)
        {
            foreach (var item in plakalar)
            {
                return item.Characters.PadRight(12);
            }
            return "";
        }

        private void resetControls()
        {
            picOriginResim.Image = null;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            resetControls();
            if (!alpr.IsLoaded())
            {
                txtPlaka.Text = "Error initializing OpenALPR";
                return;
            }
        }

        public void sqlPlateEkle(string plaka)
        {
            
            /*   if (db.plates.Where(p => p.Plate == plaka).Any())
                {  

                }*/

            if (db.plates.Where(p => p.Abone == "hayir").Any()) //abone olmayan araç giris  veya cikis yapıyor
            {

            
            if(db.plates.Where(p => p.Plate == plaka).Any() && DateTime.Compare(DateTime.Now, db.plates.FirstOrDefault(p => p.Plate == plaka).Time) > 0 && db.plates.Where(p => p.Plate == plaka).FirstOrDefault().GirisCikis == "cikis")
            {
                var giris = db.plates.Where(p => p.Plate == plaka).FirstOrDefault();
                giris.Time = DateTime.Now.AddMinutes(5);
                giris.GirisCikis = "giris";
                giris.Abone = "hayir";
                    db.SaveChanges();
            }

           
            if (db.plates.Where(p => p.Plate == plaka).Any() && DateTime.Compare(DateTime.Now, db.plates.FirstOrDefault(p => p.Plate == plaka).Time) > 0 && db.plates.Where(p => p.Plate == plaka).FirstOrDefault().GirisCikis =="giris")
            {

                //MessageBox.Show("Giris Tarihi:" + db.plates.FirstOrDefault(p => p.Plate == plaka).Time.AddMinutes(-5) + "\n" + "Cikis Tarihi:" + DateTime.Now);

                var cikis = db.plates.Where(p => p.Plate == plaka).FirstOrDefault();
                cikis.Time = DateTime.Now.AddMinutes(5);
                cikis.GirisCikis = "cikis";
                db.SaveChanges();
                    fisyazi = "Plaka: " + db.plates.FirstOrDefault(p => p.Plate == plaka).Plate
                        + "\n\nGiris:" + db.plates.FirstOrDefault(p => p.Plate == plaka).Time.AddMinutes(-5) + 
                        "\n\n" + "Cikis:" + DateTime.Now + "\n\n DOSMA OTOPARK\nMerkez, İskenderoğlu Sk. \nNo:23, 34381 Şişli/İstanbul\n(0212) 234 79 64";
                    Printing();
                }
            if (!db.plates.Where(p => p.Plate == plaka).Any())
            {
                pl.Plate = plaka;
                pl.Time = DateTime.Now.AddMinutes(5);
                pl.GirisCikis = "giris";
                pl.Abone = "hayir";
                db.plates.Add(pl);
                    db.SaveChanges();

            }
            }
            /*if(db.plates.Where(p => p.Plate == plaka).FirstOrDefault().Abone == "evet")
            {
                label1.Text = "Abone araç geçti";
            }*/
        }

        /// <summary>
        /// Video loop, parses 1 frame every tick.
        /// </summary>
        /// <param name="sender"> event sender </param>
        /// <param name="e"> args </param>
        private void LoopVideo(object sender, EventArgs e)
        {
            // Get frame
            //   Mat buffer = capture.QueryFrame();

         /*   Mat frame = new Mat();
            capture.Retrieve(frame, 0);

           picOriginResim.Image = frame; */


            //  capture.Retrieve(buffer);
            // Image<Bgr, byte> src = new Image<Bgr, byte>(buffer.Bitmap);
            //   capture.Retrieve(src);
            //   processImageFile(src);



            // Handle 'null' frame at EOF
            /*  if (buffer == null || buffer.IsEmpty)
                  {
                      timer.Enabled = false;

                      return;
                  } */





            //  Bitmap image = capture.QueryFrame().Bitmap; //take a picture



        }


        /*  private void ipCamera()
          {

              try
              {
                  // Create en open file dialog

                      capture = new VideoCapture(0);

                  //   MediaFile = fileDia.FileName;

                  if (capture.QueryFrame() != null && capture.GetCaptureProperty(CapProp.Fps) > 0.0)
                      {
                          double fps = capture.GetCaptureProperty(CapProp.Fps);

                          // Reset capture & init. timer
                          capture = new VideoCapture(0);

                          timer.Enabled = true;
                      }

              }
              catch (Exception ex)
              {
                  MessageBox.Show(ex.Message, "File error");
              }

          }*/

        private void picOriginResim_Click(object sender, EventArgs e)
        {

        }

        

        private void btnPlakayiBul_Click_1_Click(object sender, EventArgs e)
        {

          /* try
            {
                capture = new VideoCapture("rtsp://192.168.1.34:8080/video/h264");
                //  capture = new VideoCapture("rtsp://192.168.1.34:8080/video/h264");
                // capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps, 15);

                ///  capture = new VideoCapture(0);



                if (capture.QueryFrame() != null)
                {

                    // int sleepTime = (int)Math.Round(1000 / fps);
                    // Reset capture & init. timer
                    
                   // timer.Enabled = true;


                }

                // Create en open file dialog
                //OpenFileDialog fileDia = new OpenFileDialog();
                //fileDia.InitialDirectory = Environment.CurrentDirectory;
                // fileDia.Filter = "Video files (*.mp4)|*.mp4|All files (*.*)|*.*";

                //     capture = new VideoCapture(0);
                //  MediaFile = fileDia.FileName;

                //Mat image = new Mat();
                /*    while (true)
                    {


                        capture = new VideoCapture("rtsp://192.168.1.36:8080/h264_ulaw.sdp");

                        if (capture.QueryFrame() != null)
                        {

                            //  double fps = capture.GetCaptureProperty(CapProp.Fps);
                            // int sleepTime = (int)Math.Round(1000 / fps);
                            // Reset capture & init. timer
                            //capture = new VideoCapture(0);

                            capture.Read(image);
                            if (!image.IsEmpty)
                            {
                                CvInvoke.BitwiseNot(image, image);
                                //picOriginResim.Image = image;
                            }
                            // capture.Retrieve(image);

                            else
                            {
                                if (capture != null)
                                {
                                    capture.Dispose();
                                    capture = null;
                                }


                            }
                            Task t = Task.Delay(1000);
                            t.Wait();
                            // timer.Enabled = true;

                            // timer.Enabled = true;

                        }
                    } 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File error");
            }
    */
        }

        private void resimisle(object sender, EventArgs e)
        {
            
            Image<Bgr, byte> src = new Image<Bgr, byte>(capture.QuerySmallFrame().Bitmap);
            processImageFile(src);
        }
        
        private void ProcessFrame(object sender, EventArgs e)
        {

            //  Mat buffer = capture.QuerySmallFrame();

            /*   Mat frame = new Mat();
               capture.Retrieve(frame, 0);

              picOriginResim.Image = frame; */

         //   capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Gamma,20);
            //  capture.Retrieve(buffer);
         //   Image<Gray, byte> src = new Image<Gray, byte>(capture.QuerySmallFrame().Bitmap);
         //   picOriginResim.Image = capture.QuerySmallFrame();
            picOriginResim.Image = capture.QueryFrame();
            
            //   capture.Retrieve(src);
            // processImageFile(src);



            // Handle 'null' frame at EOF
            /*   if (buffer == null || buffer.IsEmpty)
                     {
                         timer.Enabled = false;

                         return;
                     } */
        }
        private void Button1_Click(object sender, EventArgs e) // araç girişi
        {
            
            if (db.plates.Where(p => p.Plate == plakaTextBox.Text.ToUpper()).Any() && 
                db.plates.Where(p => p.Plate == plakaTextBox.Text.ToUpper()).FirstOrDefault().GirisCikis == "cikis")
            {

                var giris = db.plates.Where(p => p.Plate == plakaTextBox.Text.ToUpper()).FirstOrDefault();
                giris.Time = DateTime.Now.AddMinutes(5);
                giris.GirisCikis = "giris";
                db.SaveChanges();
                label1.Text = plakaTextBox.Text.ToUpper() + " plakalı araç girişi başarılı.";
            }
            if (!db.plates.Where(p => p.Plate == plakaTextBox.Text.ToUpper()).Any())
            {
                string time = DateTime.Now.ToString();

                pl.Plate = plakaTextBox.Text.ToUpper();
                pl.Time = DateTime.Now.AddMinutes(5);
                pl.GirisCikis = "giris";
                pl.Abone = "hayir";
                db.plates.Add(pl);
                db.SaveChanges();
                label1.Text = plakaTextBox.Text.ToUpper()+ " plakalı araç girişi başarılı.";
            }




        }

        private void Button2_Click(object sender, EventArgs e) //araç çıkışı
        {
            if (db.plates.Where(p => p.Plate == plakaTextBox.Text.ToUpper()).Any() && 
                db.plates.Where(p => p.Plate == plakaTextBox.Text.ToUpper()).FirstOrDefault().GirisCikis == "giris")
            {

               //  MessageBox.Show("Plaka: " + db.plates.FirstOrDefault(p => p.Plate == plakaTextBox.Text.ToUpper()).Plate + "\nGiris Tarihi:" + db.plates.FirstOrDefault(p => p.Plate == plakaTextBox.Text.ToUpper()).Time.AddMinutes(-5) + "\n" + "Cikis Tarihi:" + DateTime.Now);
                fisyazi = "Plaka: "+ db.plates.FirstOrDefault(p => p.Plate == plakaTextBox.Text.ToUpper()).Plate + "\n\nGiris:" + db.plates.FirstOrDefault(p => p.Plate == plakaTextBox.Text.ToUpper()).Time.AddMinutes(-5) + "\n\n" + "Cikis:" + DateTime.Now + "\n\n DOSMA OTOPARK\nMerkez, İskenderoğlu Sk. \nNo:23, 34381 Şişli/İstanbul\n(0212) 234 79 64";
                Printing();
                var cikis = db.plates.Where(p => p.Plate == plakaTextBox.Text.ToUpper()).FirstOrDefault();
                cikis.Time = DateTime.Now.AddMinutes(5);
                cikis.GirisCikis = "cikis";
                db.SaveChanges();
                label1.Text = plakaTextBox.Text.ToUpper() + " plakalı araç çıkışı başarılı.";
            }
        }

        private void button3_Click(object sender, EventArgs e) //abone arac ekle
        {
            if (db.plates.Where(p => p.Plate == textBox1.Text.ToUpper()).Any()
                && db.plates.Where(p => p.Plate == textBox1.Text.ToUpper()).FirstOrDefault().Abone == "hayir")
            {
                var arac = db.plates.Where(p => p.Plate == textBox1.Text.ToUpper()).FirstOrDefault();
                arac.Time = DateTime.Now.AddMinutes(5);
                arac.GirisCikis = "cikis";
                arac.Abone = "evet";
                db.SaveChanges();
                label6.Text = "Araç abone yapıldı.";
            }
            else
            {

                label6.Text = "Araç zaten abone.";

            }
            if (!db.plates.Where(p => p.Plate == textBox1.Text.ToUpper()).Any())
            {
                pl.Plate = textBox1.Text.ToUpper();
                pl.Time = DateTime.Now.AddMinutes(5);
                pl.GirisCikis = "cikis";
                pl.Abone = "evet";
                db.plates.Add(pl);
                db.SaveChanges();
                label6.Text = "Araç abone yapıldı.";
            }
           
        }

        private void button4_Click(object sender, EventArgs e) //abone araç çıkar
        {
            if (db.plates.Where(p => p.Plate == textBox1.Text.ToUpper()).Any()
                && db.plates.Where(p => p.Plate == textBox1.Text.ToUpper()).FirstOrDefault().Abone == "evet")
            {
                var arac = db.plates.Where(p => p.Plate == textBox1.Text.ToUpper()).FirstOrDefault();
                arac.Time = DateTime.Now.AddMinutes(5);
                arac.GirisCikis = "cikis";
                arac.Abone = "hayir";
                db.SaveChanges();
                label6.Text = "Araç abonelikten çıkarıldı.";
            }
            else
            {
                label6.Text = "KAYITLI ABONE BULUNAMAMIŞTIR";
            }
        }





        public void Printing()
        {
                    printFont = new Font("Arial", 15);
                    PrintDocument pd = new PrintDocument();
                    pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);
                    // Print the document.
                    pd.Print();
        }



        private void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            float linesPerPage = 0;
            float yPos = -1000;
            int count = 0;
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;
            String line = null;

            // Calculate the number of lines per page.
               linesPerPage = ev.MarginBounds.Height /
               printFont.GetHeight(ev.Graphics);

            // Iterate over the file, printing each line.
            
                   yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));
                   ev.Graphics.DrawString(fisyazi, printFont, Brushes.Black,
                   -5,100, new StringFormat());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox2.Text = db.plates.Where(p => p.GirisCikis == "giris" && p.Time.Day == DateTime.Now.Day).Count().ToString();
            textBox3.Text = db.plates.Where(p => p.GirisCikis == "cikis" && p.Time.Day == DateTime.Now.Day).Count().ToString();
        }
    }
}
