using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

public class DBCopy : EditorWindow
{
    //Overlap
    GameObject source;

    //Copier
    GameObject target;
    bool success = true;
    bool debug = false;

    //Existing
    List<GameObject> dynamicBoneList = new List<GameObject>();
    List<GameObject> dynamicBoneColliderList = new List<GameObject>();
    List<bool> bonesForColliders = new List<bool>();
    List<bool> collidersToAdd = new List<bool>();

    //Toolbar
    int toolBar = 0;
    string[] toolBarSections = { "Copier", "Existing" };

    [MenuItem("Yelby/Dynamic Bones Copy")]
    public static void ShowWindow()
    {
        GetWindow<DBCopy>("Dynamic Bones Copy");
    }

    void OnGUI()
    {
        GUILayout.Label("Transfer [1.6]", EditorStyles.boldLabel);
        EditorGUIUtility.labelWidth = 50;

        toolBar = GUILayout.Toolbar(toolBar, toolBarSections);

        //Source
        EditorGUILayout.BeginHorizontal();
        source = EditorGUILayout.ObjectField("Source: ",source, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Set Source"))
        {
            source = Selection.activeGameObject;
            Debug.Log("Source: " + source.name);
        }
        EditorGUILayout.EndHorizontal();

        if(source != null)
            switch(toolBar)
            {
                case 0:
                    //Target
                    EditorGUILayout.BeginHorizontal();
                    target = EditorGUILayout.ObjectField("Target: ", target, typeof(GameObject), true) as GameObject;

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
                        if (success == true)
                        {
                            copyDynamicBones(source, target);
                            EditorUtility.DisplayDialog("Dynamic Bones Copier", "Successfully! " + source.name + "-->" + target.name, "Ok");
                            Debug.Log("Dynamic Bones Copier: Success");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Dynamic Bones Copier", "Failed to Copy", "Ok");
                            Debug.LogWarning("Dynamic Bones Copier: Failed");
                        }
                    }
                    if (GUILayout.Button("Destroy Target Dynamic Bones & Colliders"))
                    {
                        DestroyDynamicBones();
                        EditorUtility.DisplayDialog("Dynamic Bones Copier", "Dynamic Bones Removed", "Ok");
                    }
                    break;
                case 1:
                    dynamicBoneList.Clear();
                    dynamicBoneColliderList.Clear();

                    //Bones
                    GUILayout.Label("Dynamic Bones");
                    dynamicBoneList = ListDynamicComponents(source, dynamicBoneList, true);

                    for (int i = 0; i < dynamicBoneList.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        dynamicBoneList[i] = EditorGUILayout.ObjectField(dynamicBoneList[i], typeof(GameObject), true) as GameObject;
                        if (dynamicBoneList.Count > bonesForColliders.Count)
                            bonesForColliders.Add(default);
                        bonesForColliders[i] = EditorGUILayout.Toggle(bonesForColliders[i]);
                        GUILayout.EndHorizontal();
                    }
                    
                    //Colliders
                    GUILayout.Label("Colliders");
                    dynamicBoneColliderList = ListDynamicComponents(source, dynamicBoneColliderList, false);

                    for (int i = 0; i < dynamicBoneColliderList.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        dynamicBoneColliderList[i] = EditorGUILayout.ObjectField(dynamicBoneColliderList[i], typeof(GameObject), true) as GameObject;
                        if (dynamicBoneColliderList.Count > collidersToAdd.Count)
                            collidersToAdd.Add(default);
                        collidersToAdd[i] = EditorGUILayout.Toggle(collidersToAdd[i]);
                        GUILayout.EndHorizontal();
                    }

                    //Add colliders
                    if(GUILayout.Button("Add Colliders to Active Bone(s)"))
                    {
                        int toActiveBones = 0;
                        for (int i = 0; i < bonesForColliders.Count; i++)
                            if (bonesForColliders[i])
                                toActiveBones++;
                        if (toActiveBones == 0)
                        {
                            break;
                        }

                        int toActiveColliders = 0;
                        for (int i = 0; i < collidersToAdd.Count; i++)
                            if (collidersToAdd[i])
                                toActiveColliders++;
                        if (toActiveColliders == 0)
                        {
                            break;
                        }

                        List<DynamicBoneColliderBase> colliderList = new List<DynamicBoneColliderBase>();
                        for(int i = 0; i < collidersToAdd.Count; i++)
                        {
                            if(collidersToAdd[i])
                            {
                                colliderList.Add(dynamicBoneColliderList[i].GetComponent<DynamicBoneColliderBase>());
                            }
                        }

                        for (int i = 0; i < bonesForColliders.Count; i++)
                        {
                            if(bonesForColliders[i])
                            {
                                var boneColliderList = dynamicBoneList[i].GetComponent<DynamicBone>().m_Colliders;
                                boneColliderList.RemoveAll(item => item == null);
                                if (boneColliderList.Count == 0)
                                {
                                    boneColliderList.Add(colliderList[0]);
                                }
                                for (int j = 0; j < colliderList.Count; j++)
                                {
                                    if (!boneColliderList.Contains(colliderList[j]))
                                    {
                                        boneColliderList.Add(colliderList[j]);
                                    }

                                }
                            }
                        }
                        Debug.Log("Colliders Added");
                    }
                    break;
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
            if(debug) Debug.LogError("Collider: Multiple Bone Names [Target]");
            return;
        }
        if (debug) Debug.Log("Collider: Target Mapped [Target] " + targetBones.Count);


        //Source
        Dictionary<string, GameObject> sourceBones = new Dictionary<string, GameObject>(); //Creating dictionary
        sourceBones.Add(source.name, source); //add root to dictionary
        sourceBones = UnpackBones(source, sourceBones, "Source"); //Put bones in dictionary
        if (sourceBones.ContainsKey("CatIsEmpty"))
        {
            target.name = targetRootName;
            success = false;
            if (debug) Debug.LogError("Collider: Multiple Bone Names [Source]");
            return;
        }
        if (debug) Debug.Log("Collider: Source Mapped [Source] " + sourceBones.Count);

        foreach (var sourceBoneName in sourceBones.Keys) //Values list of GameObjects
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
        if (debug) Debug.Log("Collider: Colliders Transfered");
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
            if (debug) Debug.LogError("Dynamic: Multiple Bone Names [Target]");
            return;
        }
        if (debug) Debug.Log("Dynamic: Target Mapped [Target] " + targetBones.Count);

        //Source
        Dictionary<string, GameObject> sourceBones = new Dictionary<string, GameObject>(); //Creating dictionary
        sourceBones.Add(source.name, source); //add root to dictionary
        sourceBones = UnpackBones(source, sourceBones, "Source"); //Put bones in dictionary
        if (sourceBones.ContainsKey("CatIsEmpty"))
        {
            target.name = targetRootName;
            if (debug) Debug.LogError("Dynamic: Multiple Bone Names [Source]");
            return;
        }
        if (debug) Debug.Log("Dynamic: Source Mapped [Source] " + sourceBones.Count);

        foreach (var sourceBoneName in sourceBones.Keys) //Values list of GameObjects
        {
            if (debug) Debug.LogWarning("Dynamic: FOREACH - " + sourceBoneName);
            if (targetBones.ContainsKey(sourceBoneName)) //Bone of the same name
            {
                if (debug) Debug.LogWarning("Dynamic: IF - " + sourceBoneName);
                var targetDynamicBoneList = targetBones[sourceBoneName].GetComponents<DynamicBone>();//List of all bones on target
                for (int i = 0; i < targetDynamicBoneList.Length; i++)
                {
                    DestroyImmediate(targetDynamicBoneList[i]); //Destroy all DynamicBones on target
                }
                var sourceDynamicBoneList = sourceBones[sourceBoneName].GetComponents<DynamicBone>();//List of all bones on source
                foreach (var sourceDynamicBone in sourceDynamicBoneList)
                {
                    if (!targetBones.ContainsKey(sourceDynamicBone.m_Root.name))
                    {
                        //target.name = targetRootName;
                        continue;
                    }

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
        if (debug) Debug.LogError("Dynamic: Dynamic Bones Transfered");
    }


    //~~~~~Helper Methods~~~~~
    //Goes through skeleton
    Dictionary<string, GameObject> UnpackBones (GameObject bone, Dictionary<string, GameObject> targetBones, string name)
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

    private List<GameObject> ListDynamicComponents(GameObject source, List<GameObject> list, bool DynamicBones)
    {

        foreach(Transform obj in source.transform)
        {
            if (obj.GetComponent<DynamicBone>() && DynamicBones == true)
                list.Add(obj.gameObject);
            else if (obj.GetComponent<DynamicBoneCollider>() && DynamicBones == false)
                list.Add(obj.gameObject);
            list = ListDynamicComponents(obj.gameObject, list, DynamicBones);
        }
        return list;
    }

    private List<bool> updateList(List<GameObject> dynamicBones)
    {
        List<bool> list = new List<bool>();
        for(int i = 0; i < dynamicBones.Count; i++)
        {
            list.Add(default);
        }
        return list;
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
