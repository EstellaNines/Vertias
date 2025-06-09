using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    public Transform hand;

    // Update is called once per frame
    void Update()
    {
        var pos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        hand.right = (Vector2)(pos - hand.position).normalized;
        //Debug.Log(hand.right);
    }
}
