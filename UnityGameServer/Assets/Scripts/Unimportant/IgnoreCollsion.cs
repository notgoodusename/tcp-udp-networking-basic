using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreCollsion : MonoBehaviour
{
    // Ignore collision between players
    void Awake()
    {
        Physics.IgnoreLayerCollision(3, 3);
    }
}
