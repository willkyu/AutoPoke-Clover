using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TaskCore
{
    public bool Run();
}

public class GeneralCore : TaskCore
{
    // private 
    public bool Run()
    {
        throw new System.NotImplementedException();
    }
}

// public class CoreFactory
// {
//     public static
// }
