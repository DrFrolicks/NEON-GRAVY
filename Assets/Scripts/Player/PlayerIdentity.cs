﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Events;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;

/// <summary>
/// The high level player script.
/// Handles enabling input signal transfer.
///  handles identifying and providing the PlayerGameObject 
///  provides access to photon custom properties: gravies and kills
/// </summary>

public class PlayerIdentity : MonoBehaviourPunCallbacks
{
    #region Photon Custom Properties
    public int Gravies
    {
        get
        {
            if (photonView.Owner.CustomProperties.ContainsKey("gravies"))
                return (int)photonView.Owner.CustomProperties["gravies"];
            else
                return 0; //edge case when properties have not been initialized 
        }

        set
        {
            Hashtable h = new Hashtable { { "gravies", value } };
            photonView.Owner.SetCustomProperties(h);
            //OnGravyChange.Invoke(value); 
        }
    }

    public IntEvent OnGravyChange = new IntEvent();


    public int Kills
    {
        get
        {
            if (photonView.Owner.CustomProperties.ContainsKey("kills"))
                return (int)photonView.Owner.CustomProperties["kills"];
            else
                return 0; //edge case when properties have not been initialized 
        }

        set
        {
            Hashtable h = new Hashtable { { "kills", value } };
            photonView.Owner.SetCustomProperties(h);
        }
    }

    #endregion

    #region Implementation Values
    /// <summary>
    /// The number of hits a player can take until they are at axHitForce.
    /// The force for any given hit is calculated as hits / maxHits * maxHitForce
    /// The force will not exceed max hit force. 
    /// </summary>
    [SerializeField] public bool debugControlled;
    
    public static PlayerIdentity localPlayerInstance;
    public static GameObjectEvent OnLocalPlayerSet = new GameObjectEvent();

    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (localPlayerInstance == null)
        {
            if (!PhotonNetwork.IsConnected || photonView.AmOwner)
            {
                SetLocalPlayer();
            }
        }

        if (PhotonNetwork.IsConnected)
        {
            if (photonView.Owner == null)
                Destroy(gameObject); // this is the offline character for offline testing. 
            else 
                photonView.Owner.TagObject = gameObject;
        }
    }

    private void Start()
    {
        if(photonView.IsMine)
        {
            GetComponent<PlayerDeath>().OnDeath.AddListener(ClearGraviesAndKillsRPC); 
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!debugControlled || PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        gameObject.SendMessage("ControlledUpdate", null, SendMessageOptions.DontRequireReceiver); 
    }
    
    void FixedUpdate()
    {
        if (!debugControlled || PhotonNetwork.IsConnected && !photonView.IsMine)
            return;
        
        gameObject.SendMessage("ControlledFixedUpdate", null, SendMessageOptions.DontRequireReceiver);
    }

    void LateUpdate()
    {
        if (!debugControlled || PhotonNetwork.IsConnected && !photonView.IsMine)
            return;
        
        gameObject.SendMessage("ControlledLateUpdate", null, SendMessageOptions.DontRequireReceiver);
    }
    
    #endregion

    #region Unity Event Methods
    /// <summary>
    /// Executes the given function when the player loads, with the player's gameobject as a parameter. 
    /// </summary>
    /// <param name="func"></param>
    public static void CallOnLocalPlayerSet(UnityAction<GameObject> func)
    {
        if (localPlayerInstance == null)
        {
            OnLocalPlayerSet.AddListener(func);
        }
        else
        {
            func(localPlayerInstance.gameObject);
        }
    }
    #endregion

    #region Pun Callbacks
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        if(targetPlayer.ActorNumber == photonView.OwnerActorNr)
        {
            if (changedProps.ContainsKey("gravies"))
                OnGravyChange.Invoke((int)changedProps["gravies"]); 
        }
    }
    #endregion
    #region Custom Methods
    /// <summary>
    /// set itself as LocalPlayerInstance
    /// invokes OnLocalPlayerSet event
    /// </summary>
    void SetLocalPlayer()
    {
        localPlayerInstance = this;
        OnLocalPlayerSet.Invoke(gameObject);
        gameObject.name = "Local Player"; 
    }

    /// <summary>
    /// clears the gravies and kills custom properites  
    /// </summary>
    void ClearGraviesAndKillsRPC()
    {
        photonView.Owner.SetCustomProperties(new Hashtable() { { "gravies", 0 }, { "kills", 0 } });
    }

    #endregion

}
