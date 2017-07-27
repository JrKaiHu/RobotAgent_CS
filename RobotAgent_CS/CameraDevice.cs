using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

using System.Diagnostics;

using GigEVisionSDK_NET;
using PylonC.NET;
using PylonC.NETSupportLibrary;
using Basler.Pylon;

namespace RobotAgent_CS
{
    class CameraDevice
    {
        // for Smartek +       

        gige.IDevice m_device;
        Rectangle m_rect;
        gige.IImageProcAPI m_imageProcApi;
        PixelFormat m_pixelFormat;

        gige.IAlgorithm m_colorPipelineAlg;
        gige.IParams m_colorPipelineParams;
        gige.IResults m_colorPipelineResults;
        gige.IImageBitmap m_colorPipelineBitmap;

        gige.IAlgorithm m_changeBitDepthAlg;
        gige.IParams m_changeBitDepthParams;
        gige.IResults m_changeBitDepthResults;
        gige.IImageBitmap m_changeBitDepthBitmap;

#pragma warning disable 414
        private bool m_defaultGainNotSet;
        private double m_defaultGain;
#pragma warning restore 414

        // for Smartek -


        // for basler +

        private Camera baslerCam = null;
        private PixelDataConverter converter = null;
        private Stopwatch stopWatch = new Stopwatch();

        // for basler - 

        private Bitmap m_outBitmap = null;
        private BitmapData m_outBitmapData = null;

        private double m_dAutoExposureMin = 0.0f;
        private double m_dAutoExposureMax = 0.0f;

        private PictureBox m_imagePB;
        //private System.Windows.Forms.PictureBox m_PicBox;

        public bool m_bIsEnable = false;
        public bool m_bIsConnect = false;

        private string m_strVendor;
        private string m_strType;

        private bool m_bIsSavingFile = false;
        public CameraDevice(ComboboxItem cameraCBItem, PictureBox imagePB)
        {

            m_imagePB = imagePB;
            //m_PicBox = imagePB.PicBox;

            XDocument myXDoc = XDocument.Load("common.xml");

            XElement xeCameras = myXDoc.Root.Element("cameras");

            m_bIsEnable = int.Parse(xeCameras.Attribute("IsEnable").Value) == 1 ? true : false;

            if (m_bIsEnable)
            {

                IEnumerable<XElement> allCameras = xeCameras.Descendants("camera");

                foreach (XElement camera in allCameras)
                {

                    if (cameraCBItem.strVendor.Equals(camera.Attribute("Vendor").Value) &&
                         cameraCBItem.strType.Equals(camera.Attribute("Type").Value))
                    {

                        m_strVendor = cameraCBItem.strVendor;
                        m_strType = cameraCBItem.strType;

                        m_dAutoExposureMin = double.Parse(camera.Element("AUTO_EXPOSURE_MIN").Value);
                        m_dAutoExposureMax = double.Parse(camera.Element("AUTO_EXPOSURE_MAX").Value);
                    }
                }

                InitCamera();
            }
        }

        private void InitCamera()
        {

            if (m_strVendor.Equals("Smartek") && m_strType.Equals("GC2591CP"))
            {

                // initialize GigEVision API
                gige.GigEVisionSDK.InitGigEVisionAPI();
                gige.IGigEVisionAPI gigeVisionApi = gige.GigEVisionSDK.GetGigEVisionAPI();

                if (!gigeVisionApi.IsUsingKernelDriver())
                {
                    Console.WriteLine("Warning: Smartek Filter Driver not loaded.");
                }

                // initialize ImageProcessing API
                gige.GigEVisionSDK.InitImageProcAPI();
                m_imageProcApi = gige.GigEVisionSDK.GetImageProcAPI();

                m_colorPipelineAlg = m_imageProcApi.GetAlgorithmByName("ColorPipeline");
                m_colorPipelineAlg.CreateParams(ref m_colorPipelineParams);
                m_colorPipelineAlg.CreateResults(ref m_colorPipelineResults);
                m_imageProcApi.CreateBitmap(ref m_colorPipelineBitmap);

                m_changeBitDepthAlg = m_imageProcApi.GetAlgorithmByName("ChangeBitDepth");
                m_changeBitDepthAlg.CreateParams(ref m_changeBitDepthParams);
                m_changeBitDepthAlg.CreateResults(ref m_changeBitDepthResults);
                m_imageProcApi.CreateBitmap(ref m_changeBitDepthBitmap);

                // discover all devices on network
                gigeVisionApi.FindAllDevices(2.0);
                gige.IDevice[] devices = gigeVisionApi.GetAllDevices();

                if (devices.Length > 0)
                {

                    // take first device in list
                    m_device = devices[0];

                    if (m_device != null && m_device.Connect())
                    {

                        // disable trigger mode
                        bool status;

                        status = m_device.SetStringNodeValue("TriggerMode", "Off");
                        // set continuous acquisition mode
                        status = m_device.SetStringNodeValue("AcquisitionMode", "Continuous");
                        // start acquisition
                        status = m_device.SetIntegerNodeValue("TLParamsLocked", 1);
                        status = m_device.CommandNodeExecute("AcquisitionStart");

                        m_bIsConnect = true;
                    }
                    else
                    {

                        MessageBox.Show("Connecting to camera failed !!");
                    }
                }

                if (m_bIsConnect)
                {

                    m_defaultGainNotSet = true;
                    m_defaultGain = 0.0;
                }
                else
                {

                    Console.WriteLine("No camera connected");
                }
            }
            else if (m_strVendor.Equals("Basler") && m_strType.Equals("aca2500-14gm"))
            {

                // Destroy the old camera object.
                if (baslerCam != null)
                {

                    DestroyCamera();
                }

                // Create a new camera object.
                try
                {

                    baslerCam = new Camera();
                    converter = new PixelDataConverter();

                    baslerCam.CameraOpened += Configuration.AcquireContinuous;

                    // Register for the events of the image provider needed for proper operation.
                    baslerCam.ConnectionLost += OnConnectionLost;
                    baslerCam.CameraOpened += OnCameraOpened;
                    baslerCam.StreamGrabber.GrabStarted += OnGrabStarted;
                    baslerCam.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                    baslerCam.StreamGrabber.GrabStopped += OnGrabStopped;

                    // Open the connection to the camera device.
                    baslerCam.Open();

                    ContinuousShot();

                    m_bIsConnect = true;
                }
                catch (Exception exception)
                {

                    ShowException(exception);
                }
            }
            else
            {

                Console.WriteLine("Not support camera !!");
            }
        }

        // for basler +

        // Starts the continuous grabbing of images and handles exceptions.
        private void ContinuousShot()
        {

            try
            {

                // Start the grabbing of images until grabbing is stopped.
                baslerCam.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                baslerCam.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
            catch (Exception exception)
            {

                ShowException(exception);
            }
        }

        private void OnConnectionLost(Object sender, EventArgs e)
        {

            // Close the camera object.
            DestroyCamera();
        }

        private void OnCameraOpened(Object sender, EventArgs e)
        {

            // Enable auto exposure +

            baslerCam.Parameters[PLCamera.AutoFunctionAOISelector].SetValue(PLCamera.AutoFunctionAOISelector.AOI1);
            baslerCam.Parameters[PLCamera.AutoFunctionAOIOffsetX].SetValue(0);
            baslerCam.Parameters[PLCamera.AutoFunctionAOIOffsetY].SetValue(0);
            baslerCam.Parameters[PLCamera.AutoFunctionAOIWidth].SetValue(baslerCam.Parameters[PLCamera.Width].GetValue());
            baslerCam.Parameters[PLCamera.AutoFunctionAOIHeight].SetValue(baslerCam.Parameters[PLCamera.Height].GetValue());

            baslerCam.Parameters[PLCamera.AutoExposureTimeAbsLowerLimit].SetValue(m_dAutoExposureMin);
            baslerCam.Parameters[PLCamera.AutoExposureTimeAbsUpperLimit].SetValue(m_dAutoExposureMax);

            baslerCam.Parameters[PLCamera.AutoTargetValue].SetValue(128);

            baslerCam.Parameters[PLCamera.ExposureAuto].SetValue(PLCamera.ExposureAuto.Continuous);
            //baslerCam.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Continuous);   

            // Enable auto exposure -
        }

        private void OnGrabStarted(Object sender, EventArgs e)
        {

            stopWatch.Reset();
        }

        private void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {

            Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " OnImageGrabbed +");

            try
            {

                // Acquire the image from the camera. Only show the latest image. The camera may acquire images faster than the images can be displayed.

                // Get the grab result.
                IGrabResult grabResult = e.GrabResult;

                // Check if the image can be displayed.
                if (grabResult.IsValid)
                {

                    // Reduce the number of displayed images to a reasonable amount if the camera is acquiring images very fast.
                    if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 100)
                    {

                        Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " ElapsedMilliseconds = {0}", stopWatch.ElapsedMilliseconds);

                        stopWatch.Restart();

                        Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
                        // Lock the bits of the bitmap.
                        BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                        // Place the pointer to the buffer of the bitmap.
                        converter.OutputPixelFormat = PixelType.BGRA8packed;
                        IntPtr ptrBmp = bmpData.Scan0;
                        converter.Convert(ptrBmp, bmpData.Stride * bitmap.Height, grabResult); //Exception handling TODO

                        if (m_bIsSavingFile)
                        {

                            Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " Save Image +");

                            ImageCodecInfo jpgEncoder = GetEncoder("image/jpeg");
                            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                            EncoderParameters myEnParas = new EncoderParameters(1);
                            EncoderParameter myEnPara = new EncoderParameter(myEncoder, 90L);
                            myEnParas.Param[0] = myEnPara;
                            bitmap.Save("2.jpg", jpgEncoder, myEnParas);

                            Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " Save Image -");

                            m_bIsSavingFile = false;
                        }

                        bitmap.UnlockBits(bmpData);

                        // Assign a temporary variable to dispose the bitmap after assigning the new bitmap to the display control.
                        Bitmap bitmapOld = m_imagePB.PicBox.Image as Bitmap;

                        // Provide the display control with the new bitmap. This action automatically updates the display.
                        m_imagePB.PicBox.Image = bitmap;

                        if (bitmapOld != null)
                        {

                            // Dispose the bitmap.
                            bitmapOld.Dispose();
                        }
                    }
                }
            }
            catch (Exception exception)
            {

                ShowException(exception);
            }
            finally
            {

                // Dispose the grab result if needed for returning it to the grab loop.
                e.DisposeGrabResultIfClone();
            }

            Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " OnImageGrabbed -");
        }

        private void OnGrabStopped(Object sender, GrabStopEventArgs e)
        {

            // Reset the stopwatch.
            stopWatch.Reset();

            // If the grabbed stop due to an error, display the error message.
            if (e.Reason != GrabStopReason.UserRequest)
            {

                MessageBox.Show("A grab error occured:\n" + e.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /* Closes the image provider and handles exceptions. */
        private void DestroyCamera()
        {

            // Destroy the camera object.
            try
            {

                if (baslerCam != null)
                {

                    baslerCam.Close();
                    baslerCam.Dispose();
                    baslerCam = null;
                }
            }
            catch (Exception exception)
            {

                ShowException(exception);
            }
        }

        // Shows exceptions in a message box.
        private void ShowException(Exception exception)
        {

            MessageBox.Show("Exception caught:\n" + exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // for basler -

        public void UpdateCameraBuffer()
        {

            //Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " UpdateCameraBuffer +");

            if (m_strVendor.Equals("Smartek") && m_strType.Equals("GC2591CP"))
            {

                if (m_device != null && m_device.IsConnected())
                {

                    if (!m_device.IsBufferEmpty())
                    {

                        gige.IImageInfo imageInfo = null;
                        m_device.GetImageInfo(ref imageInfo);

                        if (imageInfo != null)
                        {

                            m_colorPipelineParams.SetBooleanNodeValue("EnableDemosaic", false);

                            double oldExposureTime, oldGain;
                            m_device.GetFloatNodeValue("ExposureTime", out oldExposureTime);
                            m_colorPipelineParams.SetFloatNodeValue("OldExposure", oldExposureTime);

                            if (m_defaultGainNotSet)
                            {

                                m_device.GetFloatNodeValue("Gain", out m_defaultGain);
                                m_colorPipelineParams.SetFloatNodeValue("DefaultGain", m_defaultGain);
                                m_colorPipelineParams.SetFloatNodeValue("OldGain", m_defaultGain);
                                oldGain = m_defaultGain;
                                m_defaultGainNotSet = false;
                            }
                            else
                            {

                                m_device.GetFloatNodeValue("Gain", out oldGain);
                                m_colorPipelineParams.SetFloatNodeValue("OldGain", oldGain);
                            }

                            UInt32 pixelType;
                            imageInfo.GetPixelType(out pixelType);
                            UInt32 bitsPerPixel = gige.GigEVisionSDK.GvspGetBitsPerPixel((gige.GVSP_PIXEL_TYPES)pixelType);
                            UInt32 targetPixelAverage = (UInt32)(Math.Pow(2.0, (Double)bitsPerPixel) / 2);
                            m_colorPipelineParams.SetFloatNodeValue("TargetPixelAverage", targetPixelAverage);
                            m_colorPipelineParams.SetFloatNodeValue("MinExposure", m_dAutoExposureMin);
                            m_colorPipelineParams.SetFloatNodeValue("MaxExposure", m_dAutoExposureMax);
                            m_colorPipelineParams.SetFloatNodeValue("ExposureTreshold", 12);
                            m_colorPipelineParams.SetFloatNodeValue("MaxGainOffset", 20);
                            m_colorPipelineParams.SetBooleanNodeValue("EnableAutoExposure", true);

                            m_imageProcApi.ExecuteAlgorithm(m_colorPipelineAlg, imageInfo, m_colorPipelineBitmap, m_colorPipelineParams, m_colorPipelineResults);

                            // new gain results
                            double redGain, greenGain, blueGain;
                            m_colorPipelineResults.GetFloatNodeValue("RedGain", out redGain);
                            m_colorPipelineResults.GetFloatNodeValue("GreenGain", out greenGain);
                            m_colorPipelineResults.GetFloatNodeValue("BlueGain", out blueGain);
                            // average pixel results
                            double avgRed, avgGreen, avgBlue;
                            m_colorPipelineResults.GetFloatNodeValue("RedAverage", out avgRed);
                            m_colorPipelineResults.GetFloatNodeValue("GreenAverage", out avgGreen);
                            m_colorPipelineResults.GetFloatNodeValue("BlueAverage", out avgBlue);

                            // autoexposure results
                            double newExposureTime, newGain;
                            m_colorPipelineResults.GetFloatNodeValue("NewExposure", out newExposureTime);
                            if (newExposureTime != oldExposureTime)
                            {

                                m_device.CommandNodeExecute("AcquisitionStop");
                                m_device.ClearImageBuffer();
                                m_device.SetFloatNodeValue("ExposureTime", newExposureTime);
                                m_device.CommandNodeExecute("AcquisitionStart");
                            }
                            m_colorPipelineResults.GetFloatNodeValue("NewGain", out newGain);
                            if ((newGain != oldGain) && (newGain > m_defaultGain))
                            {

                                m_device.CommandNodeExecute("AcquisitionStop");
                                m_device.ClearImageBuffer();
                                m_device.SetFloatNodeValue("Gain", newGain);
                                m_device.CommandNodeExecute("AcquisitionStart");
                            }

                            m_colorPipelineBitmap.GetPixelType(out pixelType);

                            if (gige.GigEVisionSDK.GvspGetBitsDepth((gige.GVSP_PIXEL_TYPES)pixelType) == 16)
                            {

                                // to show image we change bit depth to 8 bit
                                m_changeBitDepthParams.SetIntegerNodeValue("BitDepth", 8);

                                m_imageProcApi.ExecuteAlgorithm(m_changeBitDepthAlg,
                                                                 m_colorPipelineBitmap,
                                                                 m_changeBitDepthBitmap,
                                                                 m_changeBitDepthParams,
                                                                 m_changeBitDepthResults);

                                ImageUtils.CopyToBitmap(m_changeBitDepthBitmap,
                                                         ref m_outBitmap,
                                                         ref m_outBitmapData,
                                                         ref m_pixelFormat,
                                                         ref m_rect,
                                                         ref pixelType);
                            }
                            else
                            {

                                ImageUtils.CopyToBitmap(m_colorPipelineBitmap,
                                                         ref m_outBitmap,
                                                         ref m_outBitmapData,
                                                         ref m_pixelFormat,
                                                         ref m_rect,
                                                         ref pixelType);
                            }

                            if (m_outBitmap != null && m_outBitmapData != null)
                            {

                                if (m_bIsSavingFile)
                                {

                                    Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " Save Image +");

                                    ImageCodecInfo jpgEncoder = GetEncoder("image/jpeg");
                                    System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                                    EncoderParameters myEnParas = new EncoderParameters(1);
                                    EncoderParameter myEnPara = new EncoderParameter(myEncoder, 90L);
                                    myEnParas.Param[0] = myEnPara;
                                    m_outBitmap.Save("2.jpg", jpgEncoder, myEnParas);

                                    Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " Save Image -");

                                    m_bIsSavingFile = false;

                                }

                                m_imagePB.PicBox.Image = (Image)m_outBitmap;

                                m_outBitmap.UnlockBits(m_outBitmapData);
                            }
                        }
                        // remove (pop) image from image buffer
                        m_device.PopImage(imageInfo);
                        // empty buffer
                        m_device.ClearImageBuffer();
                    }
                }
            }

            else if (m_strVendor.Equals("Basler") && m_strType.Equals("aca2500-14gm"))
            {

            }
            else
            {

                Console.WriteLine("Not support camera !!");
            }

            //Console.WriteLine(DateTime.Now.ToString("[" + "HH:mm:ss:fff") + "]" + " UpdateCameraBuffer -");
        }

        private ImageCodecInfo GetEncoder(string coderType)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {

                if (codec.MimeType.Equals(coderType)) return codec;
            }

            return null;
        }

        public void SaveImage()
        {

            m_bIsSavingFile = true;
        }

        public void CloseCamera()
        {

            if (m_bIsEnable)
            {

                if (m_strVendor.Equals("Smartek") && m_strType.Equals("GC2591CP"))
                {

                    if (m_device != null && m_device.IsConnected())
                    {

                        m_device.CommandNodeExecute("AcquisitionStop");
                        m_device.SetIntegerNodeValue("TLParamsLocked", 0);
                        m_device.Disconnect();
                    }

                    m_colorPipelineAlg.DestroyParams(m_colorPipelineParams);
                    m_colorPipelineAlg.DestroyResults(m_colorPipelineResults);
                    m_imageProcApi.DestroyBitmap(m_colorPipelineBitmap);

                    m_changeBitDepthAlg.DestroyParams(m_changeBitDepthParams);
                    m_changeBitDepthAlg.DestroyResults(m_changeBitDepthResults);
                    m_imageProcApi.DestroyBitmap(m_changeBitDepthBitmap);

                    gige.GigEVisionSDK.ExitGigEVisionAPI();
                    gige.GigEVisionSDK.ExitImageProcAPI();
                }
                else if (m_strVendor.Equals("Basler") && m_strType.Equals("aca2500-14gm"))
                {

                    DestroyCamera();
                }
                else
                {

                    Console.WriteLine("Not support camera !!");
                }
            }
        }
    }
}
