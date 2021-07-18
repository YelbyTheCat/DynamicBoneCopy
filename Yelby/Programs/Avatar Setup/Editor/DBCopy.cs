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
    bool success = true;

    [MenuItem("Yelby/Dynamic Bones Copy")]
    public static void ShowWindow()
    {
        GetWindow<DBCopy>("Dynamic Bones Copy");
    }

    void OnGUI()
    {
        GUILayout.Label("Transfer [1.4]", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 50;
        //Source
        EditorGUILayout.BeginHorizontal();
        source = EditorGUILayout.ObjectField("Source: ",source, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Set Source"))
        {
            source = Selection.activeGameObject;
            Debug.Log("Source: " + source.name);
        }
        EditorGUILayout.EndHorizontal();

        //Target
        EditorGUILayout.BeginHorizontal();
        target = EditorGUILayout.ObjectField("Target: ",target, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Set Target"))
        {
            target = Selection.activeGameObject;
            Debug.Log("Target: " + target.name);
        }
        EditorGUILayout.EndHorizontal();

        //Dynamic Bone Magic
        if (GUILayout.Button("Copy/Update"))
        {
            copyColliders(source, target);
            if(success == true)
            {
                copyDynamicBones(source, target);
                EditorUtility.DisplayDialog("Dynamic Bones Copier", "Successfully! "+source.name+"-->"+target.name, "Ok");
                Debug.Log("Dynamic Bones Copier: Success");
            }
            else
            {
                EditorUtility.DisplayDialog("Dynamic Bones Copier", "Failed to Copy", "Ok");
                Debug.LogWarning("Dynamic Bones Copier: Failed");
            }
        }
        if(GUILayout.Button("Destroy Target Dynamic Bones & Colliders"))
        {
            DestroyDynamicBones();
            EditorUtility.DisplayDialog("Dynamic Bones Copier", "Dynamic Bones Removed", "Ok");
        }
    }
    //~~~~Methods~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void copyColliders(GameObject source, GameObject target)
    {
        string targetRootName = target.name;
        target.name = source.name;

        //Do Bone unpacking
        //Target
        Dictionary<string, GameObject> targetBones = new Dictionary<string, GameObject>(); //Creating dictionary
        targetBones.Add(target.name, target); //add root to dictionary
        targetBones = UnpackBones(target, targetBones, "Target"); //Put bones in dictionary
        if(targetBones.ContainsKey("CatIsEmpty"))
        {
            target.name = targetRootName;
            success = false;
            return;
        }


        //Source
        Dictionary<string, GameObject> sourceBones = new Dictionary<string, GameObject>(); //Creating dictionary
        sourceBones.Add(source.name, source); //add root to dictionary
        sourceBones = UnpackBones(source, sourceBones, "Source"); //Put bones in dictionary
        if (sourceBones.ContainsKey("CatIsEmpty"))
        {
            target.name = targetRootName;
            success = false;
            return;
        }

        foreach(var sourceBoneName in sourceBones.Keys) //Values list of GameObjects
        {
            if(targetBones.ContainsKey(sourceBoneName)) //Bone of the same name
            {
                var targetBoneColliderList = targetBones[sourceBoneName].GetComponents<DynamicBoneCollider>();//List of all bones on target
                for (int i = 0; i < targetBoneColliderList.Length; i++)
                {
                    DestroyImmediate(targetBoneColliderList[i]); //Destroy all Dynamic Bone Colliders on target
                }
                var sourceBoneColliderList = sourceBones[sourceBoneName].GetComponents<DynamicBoneCollider>();//List of all bones on source
                foreach (var sourceBoneCollider in sourceBoneColliderList)
                {
                    var newTargetBone = targetBones[sourceBoneName].AddComponent<DynamicBoneCollider>();

                    Type boneColliderType = typeof(DynamicBoneCollider);
                    FieldInfo[] boneColliderFields = boneColliderType.GetFields();
                    foreach (var field in boneColliderFields)
                    {
                        field.SetValue(newTargetBone, field.GetValue(sourceBoneCollider));
                    }
                }
            }
        }
        target.name = targetRootName;
        success = true;
    }

    void copyDynamicBones(GameObject source, GameObject target)
    {
        string targetRootName = target.name;
        target.name = source.name;

        //Do Bone unpacking
        //Target
        Dictionary<string, GameObject> targetBones = new Dictionary<string, GameObject>(); //Creating dictionary
        targetBones.Add(target.name, target); //add root to dictionary
        targetBones = UnpackBones(target, targetBones, "Target"); //Put bones in dictionary
        if (targetBones.ContainsKey("CatIsEmpty"))
        {
            target.name = targetRootName;
            return;
        }

        //Source
        Dictionary<string, GameObject> sourceBones = new Dictionary<string, GameObject>(); //Creating dictionary
        sourceBones.Add(source.name, source); //add root to dictionary
        sourceBones = UnpackBones(source, sourceBones, "Source"); //Put bones in dictionary
        if (sourceBones.ContainsKey("CatIsEmpty"))
        {
            target.name = targetRootName;
            return;
        }

        foreach (var sourceBoneName in sourceBones.Keys) //Values list of GameObjects
        {
            if (targetBones.ContainsKey(sourceBoneName)) //Bone of the same name
            {
                var targetDynamicBoneList = targetBones[sourceBoneName].GetComponents<DynamicBone>();//List of all bones on target
                for (int i = 0; i < targetDynamicBoneList.Length; i++)
                {
                    DestroyImmediate(targetDynamicBoneList[i]); //Destroy all DynamicBones on target
                }
                var sourceDynamicBoneList = sourceBones[sourceBoneName].GetComponents<DynamicBone>();//List of all bones on source
                foreach (var sourceDynamicBone in sourceDynamicBoneList)
                {
                    var newTargetBone = targetBones[sourceBoneName].AddComponent<DynamicBone>();
                    newTargetBone.m_Root = targetBones[sourceDynamicBone.m_Root.name].transform; //Sets Root

                    Type dynamicBoneType = typeof(DynamicBone);
                    FieldInfo[] dynamicBoneFields = dynamicBoneType.GetFields();
                    //General Settings
                    foreach (var field in dynamicBoneFields)
                    {
                        if (field.Name == "m_Root" || field.Name == "m_Colliders" || field.Name == "m_Exclusions")
                        {
                            continue;
                        }
                        else
                        {
                            field.SetValue(newTargetBone, field.GetValue(sourceDynamicBone));
                        }
                    }
                    //Colliders
                    newTargetBone.m_Colliders = new List<DynamicBoneColliderBase>();
                    foreach (var sourceCollider in sourceDynamicBone.m_Colliders)
                    {
                        if(sourceCollider != null)
                        {
                            string nameToFind = sourceCollider.gameObject.name;
                            if (targetBones.ContainsKey(nameToFind))
                            {
                                newTargetBone.m_Colliders.Add(targetBones[nameToFind].GetComponent<DynamicBoneColliderBase>());
                            }
                        }
                    }

                    //Exclusions
                    newTargetBone.m_Exclusions = new List<Transform>();
                    foreach (var sourceExclusion in sourceDynamicBone.m_Exclusions)
                    {
                        if(sourceExclusion != null)
                        {
                            string nameToFind = sourceExclusion.gameObject.name;
                            if (targetBones.ContainsKey(nameToFind))
                            {
                                newTargetBone.m_Exclusions.Add(targetBones[nameToFind].transform);
                            }
                        }
                    }
                }
            }
        }
        target.name = targetRootName;
    }

    //Goes through skeleton
    Dictionary<string, GameObject> UnpackBones (GameObject bone, Dictionary<string, GameObject> targetBones, String name)
    {
        foreach (Transform child in bone.transform)
        {
            if (targetBones.ContainsKey(child.name))
            {
                EditorUtility.DisplayDialog("Dynamic Bones Copier", "Duplicate Name: "+child.name + " ["+name+"]", "Ok");
                Debug.LogWarning("Dynamic Bones Copier: [Duplicate Name: "+child.name+ "]"+ "{"+name+"}");
                targetBones.Clear();
                targetBones.Add("CatIsEmpty", null);
                return targetBones;
            }
            targetBones.Add(child.name, child.gameObject);
            targetBones = UnpackBones(child.gameObject, targetBones, name); //Recursion
        }
        return targetBones;
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
                    //Debug.Log("Removed:" + child.name + " {" + child.transform.GetHierarchyPath() + "} ");
                    total++;
                }
                //Destroy Colliders
                if (child.gameObject.GetComponent<DynamicBoneCollider>() != null)
                {
                    DestroyImmediate(child.gameObject.GetComponent<DynamicBoneCollider>(), true);
                    //Debug.Log("Removed:" + child.name + " {" + child.transform.GetHierarchyPath() + "} ");
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
