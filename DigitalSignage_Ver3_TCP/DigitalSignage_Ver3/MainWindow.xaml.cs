/*
 * 作成日1/19 
 * 追記：1/25 左から来たとき、右から来たとき、人が居ない場合の画像の切り替え実装
 * 
 * 
 * 
 */

//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage backImage1;//通常時用
        BitmapImage backImage2;//右から用
        BitmapImage backImage3;//左から用
        String Normal_Text;
        String ToLeft_Text;
        String ToRight_Text;
        int stock;//一個前のステータス
        int side;//現在のステータス

        System.Net.Sockets.TcpClient tcp;
        System.Net.Sockets.NetworkStream ns;
        bool socet;
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            backImage1 = new BitmapImage(new Uri("Images/map.png", UriKind.Relative));//通常時
            backImage2 = new BitmapImage(new Uri("Images/KLspace3.png", UriKind.Relative));//右から
            backImage3 = new BitmapImage(new Uri("Images/KLspace1.png", UriKind.Relative));//左から
            stock = 0;//初期化
            side = 0;//初期化
            label1.Content = "";
            Image2.Source = backImage1;//通常画面に初期化
            socet = false;//通信ができたら(つながったら)
            Normal_Text = "";
            ToLeft_Text = "進行方向には第1食堂が近いです";
            ToRight_Text = "進行方向には第3食堂が近いです";

        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
       

            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
           
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
                
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    side = 0; 
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);
                   
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            
                            
                                if (this.SkeletonPointToScreen(skel.Joints[JointType.Head].Position).X > 320)
                                {
                                    side = 1;
                                }
                                else
                                {
                                    side = 2;

                                }
                            
                           
                            
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                          
                        }
                    }

                    //ここから主に改変
                    //label1.Content = side;//デバッグ用
                    if (side != stock)//値が変わったら(前の値と今の値で変化があったら)
                    {
                        if (stock == 0)//前の値が人が居ない状態だったら
                        {
                            if (side == 1)
                            {
                                Image2.Source = backImage2;//右からの画像に変更
                                label1.Content = ToRight_Text;//説明
                            }
                            else
                            {
                                Image2.Source = backImage3;//左からの画像に変更　
                                label1.Content = ToLeft_Text;//説明
                            }
                        }
                        else
                        {
                            if (side == 0)//前の値が人がいる状態だったら
                            {
                                Image2.Source = backImage1;//通常画像に戻る
                                label1.Content = Normal_Text;
                            }
                        }
                    }
                    stock = side;//値を記録しておく
                    
                }
                
                
               
                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
            
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        /*
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }
        */
        //終了
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        //更新ボタン
        private void button2_Click(object sender, RoutedEventArgs e)
        {
             if (socet && tcp.Available > 0)//通信ができていて、Serverから何か来てれば
             {
                 //サーバーから送られたデータを受信する
                 System.IO.MemoryStream ms = new System.IO.MemoryStream();
                 byte[] resBytes = new byte[256];
                 int resSize = 0;
                 do
                 {
                     //データの一部を受信する
                     resSize = ns.Read(resBytes, 0, resBytes.Length);
                     //Readが0を返した時はサーバーが切断したと判断
                     if (resSize == 0)
                     {
                         statusBarText.Text = "サーバーが切断しました。";
                         break;
                     }
                     //受信したデータを蓄積する
                     ms.Write(resBytes, 0, resSize);
                     //まだ読み取れるデータがあるか、データの最後が\nでない時は、
                     // 受信を続ける
                 } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');
                 //受信したデータを文字列に変換
                 System.Text.Encoding enc = System.Text.Encoding.UTF8;
                 string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                 ms.Close();
                 //末尾の\nを削除
                 //resMsg = resMsg.TrimEnd('\n');
                 //Console.WriteLine(resMsg);

                 string[] stArrayData = resMsg.Split(',');//分割
                 if (stArrayData.Length == 3)
                 {
                     Normal_Text = stArrayData[0];
                     ToLeft_Text = stArrayData[1];
                     ToRight_Text = stArrayData[2];
                 }

                 if (ns != null)
                 {
                     //閉じる
                     ns.Close();
                     tcp.Close();
                     statusBarText.Text = "通信を切断しました。";
                     socet = false;
                     //Console.ReadLine();
                 }
                 
             }
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (!socet)
            {
                try
                {
                    string ipOrHost = textBox1.Text;
                    int port = int.Parse(textBox2.Text);
                    tcp = new System.Net.Sockets.TcpClient(ipOrHost, port);
                    statusBarText.Text = "サーバー(" + ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Address + ":" + ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Port + ")と接続しました("
                            + ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Address + ":" + ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Port + ")。";
                    //NetworkStreamを取得する
                    ns = tcp.GetStream();

                    //読み取り、書き込みのタイムアウトを10秒にする
                    //デフォルトはInfiniteで、タイムアウトしない
                    //(.NET Framework 2.0以上が必要)
                    ns.ReadTimeout = 10000;
                    ns.WriteTimeout = 10000;
                    socet = true;
                }
                catch (FormatException)//入力がおかしい
                {
                    MessageBox.Show("IPアドレス、ポート番号を正しく入力してください(半角)", "エラー");
                }
                catch (System.Net.Sockets.SocketException)//
                {
                    MessageBox.Show("Serverと接続できませんでした", "エラー");
                }
            }
        }
    }
}