using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AlphaFrLgStarters
{
    private Detector detector;
    private List<Thread> threads = new List<Thread> { };
    public float speed = 1f;
    public KeyCode[] keyOptions = new KeyCode[]
    {
        KeyCode.X,
        KeyCode.Backspace,
        KeyCode.Return,
        KeyCode.UpArrow,
        KeyCode.DownArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow
    };
    private readonly System.Random rand = new System.Random();

    public AlphaFrLgStarters(Detector d)
    {
        detector = d;
    }

    private void _Run(IntPtr hwnd)
    {
        int srcWidth, srcHeight;

        while (true)
        {
            Win32Utils.SendKey(hwnd, KeyCode.X);
            Thread.Sleep(1000);

            for (int i = 0; i < 15; i++)
            {
                Win32Utils.SendKey(hwnd, KeyCode.X);
                Thread.Sleep(200);
            }

            while (detector.Detect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)).Contains("dialogue"))
            {
                Win32Utils.SendKey(hwnd, KeyCode.Z);
                Thread.Sleep(300);
            }

            while (!detector.Detect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)).Contains("dialogue"))
            {
                Thread.Sleep(300);
            }

            while (detector.Detect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)).Contains("dialogue"))
            {
                Win32Utils.SendKey(hwnd, KeyCode.Z);
                Thread.Sleep(300);
            }
            Thread.Sleep(300);
            Win32Utils.SendKey(hwnd, KeyCode.Return);
            Thread.Sleep(300);

            Win32Utils.SendKey(hwnd, KeyCode.X);
            // Thread.Sleep(100);
            while (!detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
            {
                Thread.Sleep(100);
            }

            while (detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
            {
                Thread.Sleep(300);
            }

            Thread.Sleep(300);

            Win32Utils.SendKey(hwnd, KeyCode.X);
            Thread.Sleep(400);

            Win32Utils.SendKey(hwnd, KeyCode.X);
            Thread.Sleep(300);
            while (!detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
            {
                Thread.Sleep(300);
            }

            while (detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
            {
                Thread.Sleep(300);
            }
            Thread.Sleep(600);


            if (detector.Detect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)).Contains("FrLg_s"))
            {
                break;
            }

            // SL
            Win32Utils.PressKey(hwnd, KeyCode.Z);
            Win32Utils.PressKey(hwnd, KeyCode.X);
            Win32Utils.PressKey(hwnd, KeyCode.Return);
            Win32Utils.PressKey(hwnd, KeyCode.Backspace);
            Thread.Sleep(100);
            Win32Utils.ReleaseKey(hwnd, KeyCode.Z);
            Win32Utils.ReleaseKey(hwnd, KeyCode.X);
            Win32Utils.ReleaseKey(hwnd, KeyCode.Return);
            Win32Utils.ReleaseKey(hwnd, KeyCode.Backspace);
            Thread.Sleep(rand.Next(0, 1000));


            while (!detector.Detect(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)).Contains("before_enter"))
            {
                Win32Utils.SendKey(hwnd, keyOptions[rand.Next(0, keyOptions.Length)]);
                Thread.Sleep(rand.Next(0, 300));

                Win32Utils.SendKey(hwnd, KeyCode.X);
                Thread.Sleep(rand.Next(0, 300));


            }
            Win32Utils.SendKey(hwnd, KeyCode.X);

            Thread.Sleep(500);

            Win32Utils.SendKey(hwnd, KeyCode.X);
            while (!detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
            {
                Thread.Sleep(300);
            }

            while (detector.DetectBlack(Win32Utils.CaptureWindow(hwnd, out srcWidth, out srcHeight)))
            {
                Thread.Sleep(300);
            }
            Win32Utils.SendKey(hwnd, KeyCode.Z);
            Thread.Sleep(500);
            Win32Utils.SendKey(hwnd, KeyCode.Z);
            Thread.Sleep(500);



        }


    }

    public void Run(IntPtr hwnd)
    {
        Thread _newThread = new Thread(() =>
        {
            _Run(hwnd);
        });
        threads.Add(_newThread);
        _newThread.Start();
    }

    public void End()
    {
        foreach (Thread thread in threads)
        {

            thread.Abort();
            thread.Join();

        }
    }

}