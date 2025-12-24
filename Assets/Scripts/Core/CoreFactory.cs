using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


public class CoreFactory
{
    public static TaskCore GenerateCore(APTask owner, TaskParams p, IntPtr hwnd)
    {
        return p.function switch
        {
            Function.Move => new MoveCore(hwnd, owner, p),
            Function.Fish => new FishCore(hwnd, owner, p),
            Function.Stationary => p.stationaryMode switch
            {
                StationaryMode.FrLgStarters => new FrLgStartersCore(hwnd, owner, p),
                StationaryMode.RSEStarters => new RSEStartersCore(hwnd, owner, p),
                StationaryMode.NormalHitA => new NormalHitACore(hwnd, owner, p),
                StationaryMode.Gift => new GiftCore(hwnd, owner, p),
                StationaryMode.Mew => new MewCore(hwnd, owner, p),
                _ => throw new ArgumentOutOfRangeException(
                        nameof(p.stationaryMode), p.stationaryMode, "Unsupported StationaryMode")
            },
            _ => throw new ArgumentOutOfRangeException(
                    nameof(p.function), p.function, "Unsupported Function")
        };
    }
}

