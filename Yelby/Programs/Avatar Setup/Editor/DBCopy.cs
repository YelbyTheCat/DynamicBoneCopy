using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;

public class DBCopy : EditorWindow
{
    //Attributes
    GameObject source;
    GameObject target;
    Dictionary<string, GameObject> targetBones = new Dictionary<string, GameObject>(); //Create dictionary to hold all the bones

    [MenuItem("Yelby/Dynamic Bones Copy")]
    public static void ShowWindow()
    {
        GetWindow<DBCopy>("Dynamic Bones Copy");
    }

    void OnGUI()
    {
        GUILayout.Label("Transfer", EditorStyles.boldLabel);

        //Source
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Source: ");
        source = EditorGUILayout.ObjectField(source, typeof(GameObject), false) as GameObject;

        if (GUILayout.Button("Set Source"))
        {
            source = Selection.activeGameObject;
            Debug.Log("Source: " + source.name);
        }
        EditorGUILayout.EndHorizontal();

        //Target
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Target: ");
        target = EditorGUILayout.ObjectField(target, typeof(GameObject), false) as GameObject;

        if (GUILayout.Button("Set Target"))
        {
            target = Selection.activeGameObject;
            Debug.Log("Source: " + target.name);
        }
        EditorGUILayout.EndHorizontal();

        //Dynamic Bone Magic
        if (GUILayout.Button("Copy/Update"))
        {
            copyComps(source, target);
            Debug.Log("Avatar Copy");
        }
        if(GUILayout.Button("Destroy Target Dynamic Bones & Colliders"))
        {
            DestroyDynamicBones();
        }
    }
    //~~~~Methods~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void copyComps(GameObject sTree, GameObject tTree)
    {
        //Do Magic bone work
        targetBones.Clear();
        targetBones.Add(target.name, target); //add root to dictionary
        UnpackBones(target); //Put bones in dictionary

        //Colliders
        Transform[] sourceChildren = sTree.GetComponentsInChildren<Transform>(); //List of all children
        foreach (Transform sChild in sourceChildren) //Go through all children looking for stuff
        {
            if (sChild.gameObject.GetComponent<DynamicBoneCollider>() != null)
            {
                var sChildComps = sChild.gameObject.GetComponent<DynamicBoneCollider>(); //Source componenet

                Transform[] targetChildren = tTree.gameObject.GetComponentsInChildren<Transform>(); //All target transforms
                foreach (Transform tChild in targetChildren)
                {
                    if (tChild.name == sChild.name)
                    {
                        Debug.Log("~Collider~ Source: " + sChild.name + " {" + sChild.transform.GetHierarchyPath() + "} " +
                                  "Target: " + tChild.name + " {" + tChild.transform.GetHierarchyPath() + "}");

                        //Check for Collider is exist
                        DynamicBoneCollider tChildComps = tChild.gameObject.GetComponent<DynamicBoneCollider>();
                        if (tChildComps == null)
                        {
                            tChildComps = tChild.gameObject.AddComponent<DynamicBoneCollider>();
                        }

                        //Copy Settings
                        Type type = typeof(DynamicBoneCollider);
                        FieldInfo[] info = type.GetFields();
                        foreach (var field in info)
                        {
                                field.SetValue(tChildComps, field.GetValue(sChildComps));
                        }
                    }
                }
            }
        }

        //Dynamic Bones
        foreach(Transform sChild in sourceChildren)
        { 
            if(sChild.gameObject.GetComponent<DynamicBone>() != null) //Is it a dynamic bone?
            {
                var sChildComps = sChild.gameObject.GetComponent<DynamicBone>(); //Source componenet
                
                Transform[] targetChildren = tTree.gameObject.GetComponentsInChildren<Transform>(); //All target transforms
                foreach(Transform tChild in targetChildren)
                {
                    if (tChild.name == sChild.name)
                    {
                        Debug.Log("~DynBone~ Source: " + sChild.name + " {" + sChild.transform.GetHierarchyPath() + "} " +
                                  "Target: " + tChild.name + " {" + tChild.transform.GetHierarchyPath() + "}");

                        //Find Bones if exist update
                        DynamicBone tChildComps = tChild.gameObject.GetComponent<DynamicBone>();
                        if(tChildComps == null)
                        {
                            tChildComps = tChild.gameObject.AddComponent<DynamicBone>();
                        }

                        //Dynamic Bones Resources
                        tChildComps.m_Root = tChild; //Sets Root

                        //Copy Properties
                        Type type = typeof(DynamicBone);
                        FieldInfo[] info = type.GetFields();
                        foreach(var field in info)
                        {
                            if (field.Name == "m_Root" || field.Name == "m_Colliders" || field.Name == "m_Exclusions")
                            {
                                continue;
                            }
                            else
                            {
                                field.SetValue(tChildComps, field.GetValue(sChildComps));
                            }
                        }

                        //Colliders
                        tChildComps.m_Colliders = new List<DynamicBoneColliderBase>();
                        foreach (var sourceCollider in sChildComps.m_Colliders)
                        {
                            string nameToFind = sourceCollider.gameObject.name;
                            if (sourceCollider != null && targetBones.ContainsKey(nameToFind))
                            {
                                tChildComps.m_Colliders.Add(targetBones[nameToFind].GetComponent<DynamicBoneColliderBase>());
                            }
                        }

                        //Exclusions
                        tChildComps.m_Exclusions = new List<Transform>();
                        foreach(var sourceExclusion in sChildComps.m_Exclusions)
                        {
                            string nameToFind = sourceExclusion.gameObject.name;
                            if (sourceExclusion != null && targetBones.ContainsKey(nameToFind))
                            {
                                tChildComps.m_Exclusions.Add(targetBones[nameToFind].transform);
                            }
                        }
                    }
                }
            }
        }
        AssetDatabase.Refresh();
    }

    //Goes through skeleton
    void UnpackBones (GameObject bone)
    {
        foreach(Transform child in bone.transform)
        {
            targetBones.Add(child.name, child.gameObject);
            UnpackBones(child.gameObject);//Recursion
        }
    }

    void DestroyDynamicBones()
    {
        while(true)
        {
            Transform[] allChildren = target.GetComponentsInChildren<Transform>();
            int total = 0;
            foreach (Transform child in allChildren)
            {
                //Destroy Dynamic Bones
                if (child.gameObject.GetComponent<DynamicBone>() != null)
                {
                    DestroyImmediate(child.gameObject.GetComponent<DynamicBone>(), true);
                    Debug.Log("Removed:" + child.name + " {" + child.transform.GetHierarchyPath() + "} ");
                    total++;
                }
                //Destroy Colliders
                if (child.gameObject.GetComponent<DynamicBoneCollider>() != null)
                {
                    DestroyImmediate(child.gameObject.GetComponent<DynamicBoneCollider>(), true);
                    Debug.Log("Removed:" + child.name + " {" + child.transform.GetHierarchyPath() + "} ");
                    total++;
                }
            }
            Debug.Log("Bones Removed");
            AssetDatabase.Refresh();
            if (total == 0)
                return;
        }
    }
}