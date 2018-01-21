﻿using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

using UnityEngine;
using UnityEditor;

namespace BedrockFramework.FolderImportOverride
{
    [CreateAssetMenu(fileName = "FolderAction", menuName = "BedrockFramework/FolderAction", order = 0)]
    class FolderImportOverride_Actions : SerializedScriptableObject
    {
        [SerializeField]
        private ImportOverideAction overrideAction;

        public void InvokePreAction(AssetImporter assetImporter)
        {
            overrideAction.InvokePreAction(assetImporter);
        }

        public void InvokePostAction(GameObject gameObject)
        {
            overrideAction.InvokePostAction(gameObject);
        }
    }

    public class ImportOverideAction
    {
        public virtual void InvokePreAction(AssetImporter assetImporter)
        {
        }

        public virtual void InvokePostAction(GameObject gameObject)
        {
        }
    }

    public class ImportOverideAction_SearchAndRemap : ImportOverideAction
    {
        public override void InvokePreAction(AssetImporter assetImporter)
        {
            ModelImporter modelImporter = (ModelImporter)assetImporter;
            modelImporter.SearchAndRemapMaterials(modelImporter.materialName, modelImporter.materialSearch);
        }
    }

    public class ImportOverideAction_DeleteEmptyGameObjects : ImportOverideAction
    {
        public override void InvokePostAction(GameObject gameObject)
        {
            List<GameObject> toDestroy = new List<GameObject>();

            foreach(Transform transform in gameObject.GetComponentInChildren<Transform>())
            {
                if (transform.GetComponents<Component>().Length == 1)
                    toDestroy.Add(transform.gameObject);
            }

            foreach (GameObject gameObjectToDestroy in toDestroy)
                GameObject.DestroyImmediate(gameObjectToDestroy);
        }
    }
}
