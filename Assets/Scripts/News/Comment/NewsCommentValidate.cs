﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using Mono.Data.Sqlite;
using System.Data;
using System;

/*
 * This handles the validate button that appeares when you speak in the microphone.
 * The comment will only be add to the database if the player click on this button. 
 */

namespace Valve.VR.InteractionSystem
{
    //-------------------------------------------------------------------------
    [RequireComponent(typeof(Interactable))]
    public class NewsCommentValidate : MonoBehaviour
    {
        [EnumFlags]
        [Tooltip("The flags used to attach this object to the hand.")]
        public Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.DetachFromOtherHand;

        [Tooltip("Name of the attachment transform under in the hand's hierarchy which the object should should snap to.")]
        public string attachmentPoint;

        [Tooltip("When detaching the object, should it return to its original parent?")]
        public bool restoreOriginalParent = false;

        private bool attached = false;

        public UnityEvent onPickUp;
        public UnityEvent onDetachFromHand;
        public GameObject Highlight;

        public GameObject Buttons;
        public GameObject Comment;

        // Action when you click validate, delete the two buttons
        private void ValidateAction()
        {
            // Retrieve the text of the comment and the title of the news
            var title = Comment.GetComponent<NewsComment>().titleOfNews;
            var text = Comment.GetComponent<NewsComment>().textOfComment;

            AddComment(title, text);
            Add1CommentToPlayer();

            FillId();

            Destroy(Buttons);
            // Wait 1 second to delete the validate button to not cause bug
            Destroy(gameObject, 1);
        }

        private void AddComment(string title, string text)
        {
            // Add comment to database
            string conn = "URI=file:" + Application.dataPath + "/NewsDatabase.db"; //Path to database.
            IDbConnection dbconn;
            dbconn = (IDbConnection)new SqliteConnection(conn);
            dbconn.Open(); //Open connection to the database.
            IDbCommand dbcmd = dbconn.CreateCommand();
            string sqlQuery = "INSERT INTO COMMENTS (TEXT, NEWSATTACHED, AUTHOR) VALUES(\"" + text + "\", \"" + title + "\", \"" + StaticClass.CurrentPlayerName + "\"); ";
            dbcmd.CommandText = sqlQuery;
            IDataReader reader = dbcmd.ExecuteReader();
            reader.Close();
            reader = null;
            dbcmd.Dispose();
            dbcmd = null;
            dbconn.Close();
            dbconn = null;
        }

        private void Add1CommentToPlayer()
        {
            string conn = "URI=file:" + Application.dataPath + "/NewsDatabase.db"; //Path to database.
            IDbConnection dbconn;
            dbconn = (IDbConnection)new SqliteConnection(conn);
            dbconn.Open(); //Open connection to the database.
            IDbCommand dbcmd = dbconn.CreateCommand();
            string sqlQuery = "UPDATE PLAYER SET NBRCOMMENT = NBRCOMMENT + 1 WHERE NAME = \"" + StaticClass.CurrentPlayerName + "\" ";
            dbcmd.CommandText = sqlQuery;
            IDataReader reader = dbcmd.ExecuteReader();
            reader.Close();
            reader = null;
            dbcmd.Dispose();
            dbcmd = null;
            dbconn.Close();
            dbconn = null;
        }

        private void FillId()
        {
            string conn = "URI=file:" + Application.dataPath + "/NewsDatabase.db"; //Path to database.
            IDbConnection dbconn;
            dbconn = (IDbConnection)new SqliteConnection(conn);
            dbconn.Open(); //Open connection to the database.
            IDbCommand dbcmd = dbconn.CreateCommand();
            // The Id increases automatically so if you selecte the max, you have the last one added.
            string sqlQuery = "SELECT ID FROM COMMENTS WHERE ID = (SELECT MAX(ID) FROM COMMENTS); ; ";
            dbcmd.CommandText = sqlQuery;
            IDataReader reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                int newId = reader.GetInt32(0);

                Comment.GetComponent<NewsComment>().id = newId;
                // If you just created it, it's yours so you can destroy it
                Comment.GetComponent<NewsComment>().DeleteButton.SetActive(true);
            }
            reader.Close();
            reader = null;
            dbcmd.Dispose();
            dbcmd = null;
            dbconn.Close();
            dbconn = null;
        }

        //-------------------------------------------------
        private void OnHandHoverBegin(Hand hand)
        {
            Highlight.SetActive(true);

            // "Catch" the panel by holding down the interaction button instead of pressing it.
            // Only do this if it isn't attached to another hand
            if (!attached)
            {
                if (hand.GetStandardInteractionButton())
                {
                    hand.AttachObject(gameObject, attachmentFlags, attachmentPoint);
                }
            }
        }


        //-------------------------------------------------
        private void OnHandHoverEnd(Hand hand)
        {
            Highlight.SetActive(false);

            ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        }


        //-------------------------------------------------
        private void HandHoverUpdate(Hand hand)
        {
            //Trigger got pressed
            if (!attached && hand.GetStandardInteractionButtonDown())
            {
                hand.AttachObject(gameObject, attachmentFlags, attachmentPoint);
                ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
            }
        }

        //-------------------------------------------------
        private void OnAttachedToHand(Hand hand)
        {
            attached = true;

            onPickUp.Invoke();

            hand.HoverLock(null);

            ValidateAction();
        }


        //-------------------------------------------------
        private void OnDetachedFromHand(Hand hand)
        {
            attached = false;

            onDetachFromHand.Invoke();

            hand.HoverUnlock(null);
        }


        //-------------------------------------------------
        private void HandAttachedUpdate(Hand hand)
        {
            //Trigger got released
            if (!hand.GetStandardInteractionButton())
            {
                // Detach ourselves late in the frame.
                // This is so that any vehicles the player is attached to
                // have a chance to finish updating themselves.
                // If we detach now, our position could be behind what it
                // will be at the end of the frame, and the object may appear
                // to teleport behind the hand when the player releases it.
                StartCoroutine(LateDetach(hand));
            }
        }


        //-------------------------------------------------
        private IEnumerator LateDetach(Hand hand)
        {
            yield return new WaitForEndOfFrame();

            hand.DetachObject(gameObject, restoreOriginalParent);
        }


        //-------------------------------------------------
        private void OnHandFocusAcquired(Hand hand)
        {
            gameObject.SetActive(true);
        }


        //-------------------------------------------------
        private void OnHandFocusLost(Hand hand)
        {
            gameObject.SetActive(false);
        }
    }
}
