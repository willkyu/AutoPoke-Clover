using System;
using UnityEngine;

public class SetCounterEventArgs : EventArgs
{
    public Guid guid;
    public int count;
}

public class SetFunctionEventArgs : EventArgs
{
    public Function function;
}

public class SetStationaryModeEventArgs : EventArgs
{
    public StationaryMode stationaryMode;
}

public class SetRunningEventArgs : EventArgs
{
    // public Guid guid;
    public bool running;
}

public class SetFPSEventArgs : EventArgs
{
    // public Guid guid;
    public double fps;
}