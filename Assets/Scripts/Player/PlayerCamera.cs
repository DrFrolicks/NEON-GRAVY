﻿using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private void Awake()
    {
        PlayerUserInput.CallOnLocalPlayerSet(FollowPlayer); 
    }

    void FollowPlayer(GameObject player)
    {
        GetComponent<CinemachineVirtualCamera>().Follow = player.transform;
    }
}
