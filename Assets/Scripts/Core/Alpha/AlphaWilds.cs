// using System;
// using System.Collections.Generic;
// using System.Threading;
// using UnityEngine;

// public class AlphaWilds
// {
//     private Detector detector;
//     private List<Thread> threads = new List<Thread> { };
//     public float speed = 1f;
//     public bool LR = true;
//     public KeyCode[] UpDown = new KeyCode[]
//     {
//         KeyCode.UpArrow,
//         KeyCode.DownArrow,
//     };
//     public KeyCode[] LeftRight = new KeyCode[]
//     {
//         KeyCode.LeftArrow,
//         KeyCode.RightArrow,
//     };
//     private readonly System.Random rand = new System.Random();

//     public AlphaWilds(Detector d, bool lr)
//     {
//         detector = d;
//         LR = lr;
//     }

//     private void _Run(IntPtr hwnd)
//     {
//         int srcWidth, srcHeight;
//         List<string> tempRes;

//         while (true)
//         {
//             KeyCode[] RandomKeys = LR ? LeftRight : UpDown;
//             while (!detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
//             {
//                 // Debug.Log("go");
//                 Win32Utils.SendKey(hwnd, RandomKeys[rand.Next(0, RandomKeys.Length)]);
//                 Thread.Sleep(100);
//             }

//             while (detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
//             {
//                 Thread.Sleep(300);
//             }

//             while (true)
//             {
//                 tempRes = detector.Detect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight));
//                 if (tempRes.Contains("next")) break;
//                 // Debug.Log(detector.RawDetect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight))["next"]);
//                 if (tempRes.Contains("shiny_star"))
//                 {
//                     //shiny
//                     return;
//                 }
//                 Thread.Sleep(300);
//             }

//             while (!detector.Detect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)).Contains("can_run"))
//             {
//                 Win32Utils.SendKey(hwnd, KeyCode.X);
//                 Thread.Sleep(600);

//             }
//             Thread.Sleep(300);
//             Win32Utils.SendKey(hwnd, KeyCode.RightArrow);
//             Thread.Sleep(100);

//             Win32Utils.SendKey(hwnd, KeyCode.DownArrow);
//             Thread.Sleep(100);

//             Win32Utils.SendKey(hwnd, KeyCode.X);
//             Thread.Sleep(100);

//             while (!detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
//             {
//                 Win32Utils.SendKey(hwnd, KeyCode.X);

//                 Thread.Sleep(100);
//             }

//             while (detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
//             {
//                 Thread.Sleep(300);
//             }

//         }
//     }

//     public void Run(IntPtr hwnd)
//     {
//         Thread _newThread = new Thread(() =>
//         {
//             _Run(hwnd);
//         });
//         threads.Add(_newThread);
//         _newThread.Start();
//     }

//     public void End()
//     {
//         foreach (Thread thread in threads)
//         {

//             thread.Abort();
//             thread.Join();

//         }
//     }

// }