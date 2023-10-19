/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC_Unity_Tools <https://github.com/soupday/CC_Unity_Tools>
 * 
 * CC_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using Object = UnityEngine.Object;

namespace Reallusion.Import
{
    public class Physics
    {
        const float MAX_DISTANCE = 0.1f;
        const float MAX_PENETRATION = 0.02f;
        const float WEIGHT_POWER = 1f;

        public enum PhysicsType
        {
            CollisionShapes,
            SoftPhysics
        }

        public enum ColliderType
        {
            Capsule,
            Sphere,
            Box
        }

        public enum ColliderAxis
        {
            X,
            Y,
            Z
        }

        public class CollisionShapeData
        {
            public string name;
            public string boneName;
            public bool boneActive;
            public ColliderType colliderType;
            public ColliderAxis colliderAxis;
            public float margin;
            public float friction;
            public float elasticity;
            public Vector3 translation;
            public Quaternion rotation;
            public Vector3 scale;
            public float radius;
            public float length;
            public Vector3 extent;

            public CollisionShapeData(string bone, string name, QuickJSON colliderJson)
            {
                this.name = name;
                boneName = bone;

                if (colliderJson != null)
                {
                    boneActive = colliderJson.GetBoolValue("Bone Active");
                    colliderType = (ColliderType)Enum.Parse(typeof(ColliderType), colliderJson.GetStringValue("Bound Type"));
                    colliderAxis = (ColliderAxis)Enum.Parse(typeof(ColliderAxis), colliderJson.GetStringValue("Bound Axis"));
                    margin = colliderJson.GetFloatValue("Margin");
                    friction = colliderJson.GetFloatValue("Friction");
                    elasticity = colliderJson.GetFloatValue("Elasticity");
                    translation = colliderJson.GetVector3Value("WorldTranslate");
                    rotation = colliderJson.GetQuaternionValue("WorldRotationQ");
                    scale = colliderJson.GetVector3Value("WorldScale");
                    radius = colliderJson.GetFloatValue("Radius");
                    length = colliderJson.GetFloatValue("Capsule Length");
                    if (colliderType == ColliderType.Box) extent = colliderJson.GetVector3Value("Extent");
                }
            }
        }

        public class SoftPhysicsData
        {
            public string meshName;
            public string materialName;
            public bool activate;
            public bool gravity;
            public string weightMapPath;
            public float mass;
            public float friction;
            public float damping;
            public float drag;
            public float solverFrequency;
            public float tetherLimit;
            public float Elasticity;
            public float stretch;
            public float bending;
            public Vector3 inertia;
            public bool softRigidCollision;
            public float softRigidMargin;
            public bool selfCollision;
            public float selfMargin;
            public float stiffnessFrequency;
            public bool isHair;

            public SoftPhysicsData(string mesh, string material, QuickJSON softPhysicsJson, QuickJSON characterJson)
            {
                meshName = mesh;
                materialName = material;
                isHair = false;
                QuickJSON objectMaterialJson = characterJson.GetObjectAtPath("Meshes/" + mesh + "/Materials/" + material);
                if (objectMaterialJson != null &&
                    objectMaterialJson.PathExists("Custom Shader/Shader Name") &&
                    objectMaterialJson.GetStringValue("Custom Shader/Shader Name") == "RLHair")
                    isHair = true;

                if (softPhysicsJson != null)
                {
                    activate = softPhysicsJson.GetBoolValue("Activate Physics");
                    gravity = softPhysicsJson.GetBoolValue("Use Global Gravity");
                    weightMapPath = softPhysicsJson.GetStringValue("Weight Map Path");
                    mass = softPhysicsJson.GetFloatValue("Mass");
                    friction = softPhysicsJson.GetFloatValue("Friction");
                    damping = softPhysicsJson.GetFloatValue("Damping");
                    drag = softPhysicsJson.GetFloatValue("Drag");
                    solverFrequency = softPhysicsJson.GetFloatValue("Solver Frequency");
                    tetherLimit = softPhysicsJson.GetFloatValue("Tether Limit");
                    Elasticity = softPhysicsJson.GetFloatValue("Elasticity");
                    stretch = softPhysicsJson.GetFloatValue("Stretch");
                    bending = softPhysicsJson.GetFloatValue("Bending");
                    inertia = softPhysicsJson.GetVector3Value("Inertia");
                    softRigidCollision = softPhysicsJson.GetBoolValue("Soft Vs Rigid Collision");
                    softRigidMargin = softPhysicsJson.GetFloatValue("Soft Vs Rigid Collision_Margin");
                    selfCollision = softPhysicsJson.GetBoolValue("Self Collision");
                    selfMargin = softPhysicsJson.GetFloatValue("Self Collision Margin");
                    stiffnessFrequency = softPhysicsJson.GetFloatValue("Stiffness Frequency");
                }
            }
        }

        public static float PHYSICS_SHRINK_COLLIDER_RADIUS
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Physics_Shrink_Collider_Radius"))
                    return EditorPrefs.GetFloat("RL_Physics_Shrink_Collider_Radius");
                return 0.5f;
            }

            set
            {
                EditorPrefs.SetFloat("RL_Physics_Shrink_Collider_Radius", value);
            }
        }

        public static float PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Physics_Weight_Map_Collider_Detect_Threshold"))
                    return EditorPrefs.GetFloat("RL_Physics_Weight_Map_Collider_Detect_Threshold");
                return 0.5f;
            }

            set
            {
                EditorPrefs.SetFloat("RL_Physics_Weight_Map_Collider_Detect_Threshold", value);
            }
        }

        // bones that can have spring bone colliders
        private List<string> GetVaildSpringBoneColliders()
        {
            // <<<Magic>>>

            // use a placeholder fixed list 
            return springColliderBones;
        }

        private List<string> springColliderBones = new List<string> {
            "CC_Base_Head", "CC_Base_Spine01", "CC_Base_NeckTwist01", "CC_Base_R_Upperarm", "CC_Base_L_Upperarm",
        };

        private GameObject prefabInstance;
        private float modelScale = 0.01f;
        private bool addClothPhysics = false;
        private bool addUnityClothPhysics = false;
        private bool addMagicaClothPhysics = false;
        private bool addHairPhysics = false;
        private bool addUnityHairPhysics = false;
        private bool addHairSpringBones = false;
        private bool addMagicaHairSpringBones = false;

        private List<CollisionShapeData> boneColliders;
        private List<SoftPhysicsData> softPhysics;
        private List<GameObject> clothMeshes;

        private string characterName;
        private string fbxFolder;
        private string characterGUID;
        private List<string> textureFolders;
        private QuickJSON jsonData;
        private bool aPose;
        private CharacterInfo characterInfo;
        private const int MAGICA_WEIGHT_SIZE = 128;

        public Physics(CharacterInfo info, GameObject prefabInstance)
        {
            characterInfo = info;
            this.prefabInstance = prefabInstance;
            boneColliders = new List<CollisionShapeData>();
            softPhysics = new List<SoftPhysicsData>();
            clothMeshes = new List<GameObject>();
            modelScale = 0.01f;
            fbxFolder = info.folder;
            characterGUID = info.guid;
            characterName = info.name;
            fbxFolder = info.folder;
            jsonData = info.JsonData;
            addClothPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.ClothPhysics) > 0;
            addUnityClothPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.UnityClothPhysics) > 0;
            addMagicaClothPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.MagicaCloth) > 0;
            addHairPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.HairPhysics) > 0;
            addUnityHairPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics) > 0;
            addHairSpringBones = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.SpringBoneHair) > 0;
            addMagicaHairSpringBones = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.MagicaBone) > 0;
            string fbmFolder = Path.Combine(fbxFolder, characterName + ".fbm");
            string texFolder = Path.Combine(fbxFolder, "textures", characterName);
            textureFolders = new List<string>() { fbmFolder, texFolder };
            if (info.CharacterJsonData.PathExists("Bind_Pose"))
            {
                if (info.CharacterJsonData.GetStringValue("Bind_Pose") == "APose") aPose = true;
            }

            ReadPhysicsJson(info.PhysicsJsonData, info.CharacterJsonData);
        }

        private void ReadPhysicsJson(QuickJSON physicsJson, QuickJSON characterJson)
        {
            QuickJSON shapesJson = physicsJson.GetObjectAtPath("Collision Shapes");
            QuickJSON softPhysicsJson = physicsJson.GetObjectAtPath("Soft Physics/Meshes");

            if (shapesJson != null)
            {
                boneColliders.Clear();

                foreach (MultiValue boneJson in shapesJson.values)
                {
                    string boneName = boneJson.Key;
                    QuickJSON collidersJson = boneJson.ObjectValue;
                    if (collidersJson != null)
                    {
                        foreach (MultiValue colliderJson in collidersJson.values)
                        {
                            string colliderName = colliderJson.Key;
                            if (colliderJson.ObjectValue != null)
                                boneColliders.Add(new CollisionShapeData(boneName, colliderName, colliderJson.ObjectValue));
                        }
                    }
                }
            }

            if (softPhysicsJson != null)
            {
                softPhysics.Clear();

                foreach (MultiValue meshJson in softPhysicsJson.values)
                {
                    string meshName = meshJson.Key;
                    QuickJSON physicsMaterialsJson = meshJson.ObjectValue.GetObjectAtPath("Materials");
                    if (physicsMaterialsJson != null)
                    {
                        foreach (MultiValue matJson in physicsMaterialsJson.values)
                        {
                            string materialName = matJson.Key;
                            if (matJson.ObjectValue != null)
                                softPhysics.Add(new SoftPhysicsData(meshName, materialName, matJson.ObjectValue, characterJson));
                        }
                    }
                }
            }
        }

        public void AddPhysics(bool applyInstance)
        {
#if UNITY_2020_1_OR_NEWER
            AddCollidersToPrefabContents();
#else
            AddCollidersToPrefabInstance();
#endif
            AddCloth();
            AddSpringBones();

            if (applyInstance) PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
        }

        public void RemoveAllPhysics()
        {
            Collider[] colliders = prefabInstance.GetComponentsInChildren<Collider>();
        }

        private bool MAGICA_CLOTH_AVAILABLE = false;
        private bool DYNAMIC_BONE_AVAILABLE = false;

#if UNITY_2020_1_OR_NEWER
        // post 2020.1 version - the PrefabUtility class was updated for 2020.1 
        // uses a disposable helper struct for automatically loading the contents of a Prefab file, saving the contents and unloading the contents again.
        // see: https://docs.unity3d.com/ScriptReference/PrefabUtility.EditPrefabContentsScope.html
        private void AddCollidersToPrefabContents()
        {
            MAGICA_CLOTH_AVAILABLE = MagicaCloth2IsAvailable();
            DYNAMIC_BONE_AVAILABLE = DynamicBoneIsAvailable();

            // edit within the character prefab
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(ImporterWindow.Current.Character.PrefabAsset)))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                PurgeAllPhysicsComponents(prefabRoot);

                AddCollidersToPrefabRoot(prefabRoot);
            }
        }
#endif
        // pre 2020.1 legacy version which includes MagicaCloth and DynamicBone
        // the PrefabUtility class was updated for 2020.1 - see this for early discussion:
        // https://forum.unity.com/threads/how-do-i-edit-prefabs-from-scripts.685711/#post-4591465
        // This method uses LoadPrefabContents which: "Loads a Prefab Asset at a given path into an isolated Scene and returns the root GameObject of the Prefab."
        private void AddCollidersToPrefabInstance()
        {
            MAGICA_CLOTH_AVAILABLE = MagicaCloth2IsAvailable();
            DYNAMIC_BONE_AVAILABLE = DynamicBoneIsAvailable();

            string currentPrefabAssetPath = AssetDatabase.GetAssetPath(ImporterWindow.Current.Character.PrefabAsset);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(currentPrefabAssetPath);
            PurgeAllPhysicsComponents(prefabRoot);

            AddCollidersToPrefabRoot(prefabRoot);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, currentPrefabAssetPath, out bool success);
            Debug.Log("Prefab Asset: " + currentPrefabAssetPath + (success ? " successfully saved." : " failed to save."));
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private void AddCollidersToPrefabRoot(GameObject prefabRoot)
        {
            Transform[] objects = prefabRoot.GetComponentsInChildren<Transform>(true);
            Dictionary<Object, string> colliderLookup = new Dictionary<Object, string>();
            Dictionary<Collider, Collider> existingLookup = new Dictionary<Collider, Collider>();

            // delegates
            Func<string, Transform> FindBone = (boneName) => Array.Find(objects, o => o.name.Equals(boneName));
            Func<string, Transform, Collider> FindColliderObj = (colliderName, bone) =>
            {
                for (int i = 0; i < bone.childCount; i++)
                {
                    Transform child = bone.GetChild(i);
                    if (child.name == colliderName)
                    {
                        return child.GetComponent<Collider>();
                    }
                }
                return null;
            };

            GameObject parent = new GameObject("Temporary GameObject");
            parent.transform.SetParent(prefabRoot.transform, false);

            List<string> validSpringBoneColliders = GetVaildSpringBoneColliders();

            foreach (CollisionShapeData collider in boneColliders)
            {
                string colliderName = collider.boneName + "_" + collider.name;
                GameObject g = GetColliderGameObject(colliderName, objects);
                g.transform.SetParent(parent.transform);
                Transform t = g.transform;
                t.position = collider.translation * modelScale;
                t.rotation = collider.rotation;

                bool boneValid = validSpringBoneColliders.Contains(collider.boneName);
                bool addFullColliderSet = true; // placeholder

                if (addMagicaClothPhysics)
                {
                    if (MAGICA_CLOTH_AVAILABLE)
                    {
                        Object c = AddMagicaCloth2Collider(g, collider);
                        colliderLookup.Add(c, collider.boneName);
                    }
                }
                else if ((addMagicaHairSpringBones && boneValid) || (addMagicaHairSpringBones && addFullColliderSet))
                {
                    if (MAGICA_CLOTH_AVAILABLE)
                    {
                        Object c = AddMagicaCloth2Collider(g, collider);
                        colliderLookup.Add(c, collider.boneName);
                    }
                }

                if (addUnityClothPhysics)
                {
                    Object c = AddNativeCollider(g, collider);
                    colliderLookup.Add(c, collider.boneName);
                }
                else if ((addUnityHairPhysics && boneValid) || (addUnityHairPhysics && addFullColliderSet))
                {
                    Object c = AddNativeCollider(g, collider);
                    colliderLookup.Add(c, collider.boneName);
                }

                if (addHairSpringBones && boneValid)
                {
                    Debug.Log("DYNAMIC BONE - TESTING AGAINST: " + collider.boneName);
                    Object c = AddDynamicBoneCollider(g, collider);
                    colliderLookup.Add(c, collider.boneName);
                }
            }
            // rotate all the transform data from its original space (JSON) into the Unity coordinate system
            parent.transform.Rotate(Vector3.left, 90);
            parent.transform.localScale = new Vector3(-1f, 1f, 1f);

            if (aPose) FixColliderAPose(objects, colliderLookup);

            // as the transforms have moved, need to re-sync the transforms in the physics engine
            UnityEngine.Physics.SyncTransforms();

            List<Object> listColliders = new List<Object>(colliderLookup.Count);

            foreach (KeyValuePair<Object, string> collPair in colliderLookup)
            {
                Component c = collPair.Key as Component;
                if (c)
                {
                    Transform t = c.transform;

                    string colliderBone = collPair.Value;

                    Transform bone = FindBone(colliderBone);
                    if (bone)
                    {
                        // reparent with keep position
                        t.transform.SetParent(bone, true);
                        // add to list of colliders
                        listColliders.Add(t);
                    }
                }
            }
            GameObject.DestroyImmediate(parent);

            // add collider manager to prefab root
            ColliderManager colliderManager = prefabRoot.GetComponent<ColliderManager>();
            if (colliderManager == null) colliderManager = prefabRoot.AddComponent<ColliderManager>();

            // add colliders to manager
            if (colliderManager)
            {
                colliderManager.characterGUID = characterGUID;
                colliderManager.magicaCloth2Available = MAGICA_CLOTH_AVAILABLE;
                colliderManager.dynamicBoneAvailable = DYNAMIC_BONE_AVAILABLE;
                colliderManager.AddColliders(listColliders);
            }

            SaveReferenceAbstractColliders(colliderManager);            
        }

        public GameObject GetColliderGameObject(string colliderName, Transform[] prefabObjects)
        {
            // if there is an existing matching colider object inside the prefab then
            // clean the native, magica and dynamic bone colliders and return the gameobject
            // this should preserve any references to the collider gameobject
            foreach (Transform childtransform in prefabObjects)
            {
                if (childtransform.name == colliderName)
                {
                    GameObject colliderGameObject = childtransform.gameObject;

                    // if any collider objects have been disabled then re-enable them for rebuild
                    if (!colliderGameObject.activeInHierarchy)
                        colliderGameObject.SetActive(true);

                    if (MAGICA_CLOTH_AVAILABLE)
                    {
                        var magicaColliderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");
                        if (magicaColliderType != null)
                        {
                            var mCol = colliderGameObject.GetComponent(magicaColliderType);
                            if (mCol)
                            {
                                GameObject.DestroyImmediate(mCol);
                            }
                        }
                    }

                    if (DYNAMIC_BONE_AVAILABLE)
                    {
                        var dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneCollider");
                        if (dynamicBoneColliderType != null)
                        {
                            var dCol = colliderGameObject.GetComponent(dynamicBoneColliderType);
                            if (dCol)
                            {
                                GameObject.DestroyImmediate(dCol);
                            }
                        }
                    }

                    var col = colliderGameObject.GetComponent<Collider>();
                    if (col != null)
                    {
                        GameObject.DestroyImmediate(col);
                    }
                    return colliderGameObject;
                }
            }
            return new GameObject(colliderName);
        }

        private void PurgeAllPhysicsComponents(GameObject prefabRoot)
        {
            //Debug.Log("PurgeAllPhysicsComponents from: " + prefabRoot.name);

            Type dynamicBoneType = GetTypeInAssemblies("DynamicBone");
            Type magicaClothType = GetTypeInAssemblies("MagicaCloth2.MagicaCloth");

            Transform[] objects = prefabRoot.GetComponentsInChildren<Transform>();
            foreach (Transform t in objects)
            {
                GameObject g = t.gameObject;
                var clothInstance = g.GetComponent<Cloth>();
                if (clothInstance != null)
                {
                    //Debug.Log("Removing Cloth instance from: " + t.name);
                    GameObject.DestroyImmediate(clothInstance);
                }

                var weightMapperInstance = g.GetComponent<WeightMapper>();
                if (weightMapperInstance != null)
                {
                    //Debug.Log("Removing WeightMapper instance from: " + t.name);
                    GameObject.DestroyImmediate(weightMapperInstance);
                }

                var colliderManagerInstance = g.GetComponent<ColliderManager>();
                if (colliderManagerInstance != null)
                {
                    //Debug.Log("Removing ColliderManager instance from: " + t.name);
                    GameObject.DestroyImmediate(colliderManagerInstance);
                }

                if (magicaClothType != null)
                {
                    var magicaClothInstance = g.GetComponent(magicaClothType);
                    if (magicaClothInstance != null)
                    {
                        //Debug.Log("Removing MagicaCloth instance from: " + t.name);
                        GameObject.DestroyImmediate(magicaClothInstance);
                    }
                }

                if (dynamicBoneType != null)
                {
                    var dynamicBoneInstance = g.GetComponent(dynamicBoneType);
                    if (dynamicBoneInstance != null)
                    {
                        GameObject.DestroyImmediate(dynamicBoneInstance);
                    }
                }
            }
        }

        public void SaveReferenceAbstractColliders(ColliderManager colliderManager)
        {
            // create a reference list of abstract colliders to be used as a 'reset to defaults' resource             
            CreateAbstractColliders(colliderManager, out List<ColliderManager.AbstractCapsuleCollider> abstractColliders);
            //Debug.Log("Build Time abstract count = " + abstractColliders.Count);
            PhysicsSettingsStore.SaveAbstractColliderSettings(colliderManager, abstractColliders, true);
        }

        private void AddSpringBones()
        {
            if (addHairPhysics)
            {
                if (addHairSpringBones)
                    AddDynamicBoneSpringBones();

                if (addMagicaHairSpringBones)
                    AddMagicaBoneCloth();
            }
        }

        private void AddDynamicBoneSpringBones()
        {
            Type dynamicBoneType = GetTypeInAssemblies("DynamicBone");

            if (!addHairSpringBones && dynamicBoneType != null)
            {
                var existingDynamicBoneComponent = prefabInstance.GetComponent(dynamicBoneType);
                if (existingDynamicBoneComponent != null)
                {
                    Component.DestroyImmediate(existingDynamicBoneComponent);
                }
                return;
            }

            if (!addHairSpringBones) return;

            if (dynamicBoneType == null)
            {
                Debug.LogWarning("Warning: DynamicBone not found in project assembly.");
                return;
            }

            // remove old dynamic bone components
            var dynamicBoneComponents = prefabInstance.GetComponents(dynamicBoneType);
            Util.LogInfo("Removing: " + dynamicBoneComponents.Length + " DynamicBone Components");
            foreach (var dbc in dynamicBoneComponents)
            {
                Component.DestroyImmediate(dbc);
            }

            // find all spring rigs
            List<GameObject> springRigs = new List<GameObject>();
            MeshUtil.FindCharacterBones(prefabInstance, springRigs, "RL_Hair_Rig_", "RLS_");

            // build all spring rigs
            foreach (GameObject rigBone in springRigs)
            {
                Util.LogInfo("Processing Spring Bone Rig: " + rigBone.name);

                var dynamicBoneComponent = prefabInstance.AddComponent(dynamicBoneType);

                if (dynamicBoneComponent == null)
                {
                    Debug.LogError("Unable to add DynamicBone Component!");
                    return;
                }

                List<Transform> hairRoots = new List<Transform>();

                for (int i = 0; i < rigBone.transform.childCount; i++)
                {
                    Transform childBone = rigBone.transform.GetChild(i);
                    hairRoots.Add(childBone);
                }

                if (hairRoots.Count > 0)
                {
                    Util.LogInfo("Found: " + hairRoots.Count + " spring bone chains");
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_Roots", hairRoots);
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_Damping", 0.11f);
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_Elasticity", 0.04f);
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_Stiffness", 0.33f);
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_Radius", 0.02f);
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_EndLength", 0f);
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_Gravity", new Vector3(0f, -0.0098f, 0f));
                    SetTypeField(dynamicBoneType, dynamicBoneComponent, "m_UpdateMode", 0);
                }

                Type dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneColliderBase");
                FieldInfo fColliders = dynamicBoneType.GetField("m_Colliders");
                MethodInfo mCollidersClear = fColliders.FieldType.GetMethod("Clear");
                MethodInfo mCollidersAdd = fColliders.FieldType.GetMethod("Add");
                var colliders = fColliders.GetValue(dynamicBoneComponent);
                if (colliders == null)
                {
                    dynamic o = CreateGeneric(typeof(List<>), dynamicBoneColliderType);
                    fColliders.SetValue(dynamicBoneComponent, o);
                    colliders = fColliders.GetValue(dynamicBoneComponent);
                }

                mCollidersClear.Invoke(colliders, null);

                IList dynamicBoneColliders = FetchDynamicBoneColliders(prefabInstance, GetVaildSpringBoneColliders());
                //Debug.Log ("DYNAMIC BONE COLLIDER LIST CONTAINS: " + dynamicBoneColliders.Count);
                foreach (var dynamicBoneCollider in dynamicBoneColliders)
                {
                    mCollidersAdd.Invoke(colliders, new object[] { dynamicBoneCollider });
                }

                /*
                ColliderManager colliderManager = prefabInstance.GetComponent<ColliderManager>();
                if (colliderManager)
                {
                    foreach (Collider c in colliderManager.colliders)
                    {
                        var dynamicBoneColliderComponent = c.gameObject.GetComponent(dynamicBoneColliderType);
                        if (dynamicBoneColliderComponent != null)
                        {
                            mCollidersAdd.Invoke(colliders, new object[] { dynamicBoneColliderComponent });
                        }
                    }
                }
                */
            }
        }

        /// See: https://stackoverflow.com/questions/10754150/dynamic-type-with-lists-in-c-sharp
        public static object CreateGeneric(Type generic, Type innerType, params object[] args)
        {
            System.Type specificType = generic.MakeGenericType(new System.Type[] { innerType });
            return Activator.CreateInstance(specificType, args);
        }

        private void AddCloth()
        {
            clothMeshes.Clear();

            PrepWeightMaps();

            //
            if (MagicaCloth2IsAvailable() && addMagicaClothPhysics)
                AddMagicaMeshCloth();
            //
            if (addUnityClothPhysics || addUnityHairPhysics)
            {
                List<string> hairMeshNames = FindHairMeshes();
                Transform[] transforms = prefabInstance.GetComponentsInChildren<Transform>();
                foreach (Transform t in transforms)
                {
                    GameObject obj = t.gameObject;
                    //Debug.Log(obj.name);
                    foreach (SoftPhysicsData data in softPhysics)
                    {
                        string meshName = obj.name;
                        if (meshName.iContains("_Extracted"))
                        {
                            meshName = meshName.Remove(meshName.IndexOf("_Extracted"));
                        }

                        if (meshName == data.meshName)
                        {
                            if (CanAddPhysics(meshName, hairMeshNames))
                            {
                                //Debug.Log("AddCloth CanAddPhysics: " + obj.name + " " + data.meshName);
                                if (!data.isHair && addUnityClothPhysics)
                                {
                                    DoCloth(obj, meshName);
                                    clothMeshes.Add(obj);
                                }
                                
                                if (data.isHair && addUnityHairPhysics)
                                {
                                    DoCloth(obj, meshName);
                                    clothMeshes.Add(obj);
                                }
                            }
                            else
                            {
                                RemoveCloth(obj);
                            }
                        }
                    }
                }

                ColliderManager colliderManager = prefabInstance.GetComponent<ColliderManager>();
                if (colliderManager)
                {
                    colliderManager.clothMeshes = clothMeshes.ToArray();
                }
            }
        }

        private bool CanAddPhysics(SoftPhysicsData data)
        {
            if (data != null)
            {
                if (data.isHair)
                {
                    //Debug.Log("CanAddPhysics (isHair) to " + data.meshName);
                    if (addHairPhysics) return true;
                }
                else
                {
                    //Debug.Log("CanAddPhysics (is NOT Hair) to " + data.meshName);
                    if (addClothPhysics) return true;
                }
            }

            return false;
        }

        private bool CanAddPhysics(string meshName, List<string> hairMeshNames)
        {
            if (hairMeshNames.iContains(meshName))
            {
                return addHairPhysics;
            }
            else
            {
                return addClothPhysics;
            }
        }

        private List<string> FindHairMeshes()
        {
            List<string> hairMeshNames = new List<string>();
            Transform[] transforms = prefabInstance.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                GameObject obj = t.gameObject;
                foreach (SoftPhysicsData data in softPhysics)
                {
                    string meshName = obj.name;
                    if (meshName.iContains("_Extracted"))
                    {
                        meshName = meshName.Remove(meshName.IndexOf("_Extracted"));
                    }

                    if (meshName == data.meshName)
                    {
                        if (data.isHair && !hairMeshNames.Contains(meshName))
                            hairMeshNames.Add(meshName);
                    }
                }
            }

            return hairMeshNames;
        }

        private void PrepWeightMaps()
        {
            if (addClothPhysics || addHairPhysics)
            {
                foreach (SoftPhysicsData data in softPhysics)
                {
                    Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                    SetWeightMapImport(weightMap);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void DoCloth(GameObject clothTarget, string meshName)
        {
            //Debug.Log("DoCloth : " + clothTarget.name + " " +  meshName);
            SkinnedMeshRenderer renderer = clothTarget.GetComponent<SkinnedMeshRenderer>();
            if (!renderer) return;
            Mesh mesh = renderer.sharedMesh;
            if (!mesh) return;

            List<WeightMapper.PhysicsSettings> settingsList = new List<WeightMapper.PhysicsSettings>();

            bool hasPhysics = false;

            for (int i = 0; i < mesh.subMeshCount; i++)//
            {
                Material mat = renderer.sharedMaterials[i];

                if (!mat) continue;
                //Debug.Log("sharedMaterials " + mat.name);
                string sourceName = mat.name;
                if (sourceName.iContains("_2nd_Pass")) continue;
                if (sourceName.iContains("_1st_Pass"))
                {
                    sourceName = sourceName.Remove(sourceName.IndexOf("_1st_Pass"));
                }

                foreach (SoftPhysicsData data in softPhysics)
                {
                    //Debug.Log("SoftPhysicsData data " + data.meshName);
                    if (data.materialName == sourceName &&
                        data.meshName == meshName &&
                        CanAddPhysics(data))
                    {
                        WeightMapper.PhysicsSettings settings = new WeightMapper.PhysicsSettings();

                        settings.name = sourceName;
                        settings.activate = data.activate;
                        settings.gravity = data.gravity;
                        settings.selfCollision = Importer.USE_SELF_COLLISION ? data.selfCollision : false;
                        settings.softRigidCollision = data.softRigidCollision;
                        settings.softRigidMargin = data.softRigidMargin;

                        if (data.isHair)
                        {
                            // hair meshes degenerate quickly if less than full stiffness 
                            // (too dense, too many verts?)
                            settings.bending = 0f;
                            settings.stretch = 0f;
                        }
                        else
                        {
                            settings.bending = data.bending;
                            settings.stretch = data.stretch;
                        }

                        settings.solverFrequency = data.solverFrequency;
                        settings.stiffnessFrequency = data.stiffnessFrequency;
                        settings.mass = data.mass;
                        settings.friction = data.friction;
                        settings.damping = data.damping;
                        settings.selfMargin = data.selfMargin;
                        settings.maxDistance = 20f;
                        settings.maxPenetration = 10f;
                        settings.colliderThreshold = PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD;

                        Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                        if (!weightMap) weightMap = Texture2D.blackTexture;
                        settings.weightMap = weightMap;

                        settingsList.Add(settings);
                        hasPhysics = true;
                    }
                }
            }

            if (hasPhysics)
            {
                WeightMapper mapper = clothTarget.GetComponent<WeightMapper>();
                if (!mapper) mapper = clothTarget.AddComponent<WeightMapper>();

                mapper.settings = settingsList.ToArray();
                mapper.characterGUID = characterGUID;
                mapper.ApplyWeightMap();
            }
        }

        private void AddMagicaMeshCloth()
        {
            // construct a single instance of magica cloth
            var cloth = AddMagicaClothInstance(0);

            // add relevant skinned mesh renderers along with converted weight maps
            // add a list of colliders that can interact with the cloth instance
            Type clothType = GetTypeInAssemblies("MagicaCloth2.MagicaCloth");
            if (clothType != null)
            {
                Transform[] transforms = prefabInstance.GetComponentsInChildren<Transform>();
                foreach (Transform t in transforms)
                {
                    GameObject obj = t.gameObject;
                    foreach (SoftPhysicsData data in softPhysics)
                    {
                        string meshName = obj.name;
                        // omit hair for the moment
                        if (!data.isHair)
                        {
                            if (meshName == data.meshName)
                            {
                                //Debug.Log("ADDMAGICACLOTH:: " + obj.name);
                                DoMagicaCloth(cloth, obj, data);
                            }
                        }
                    }
                }

                IList magicaColliderList = FetchMagicaColliders(prefabInstance.gameObject);

                var serializedDataProperty = cloth.GetType().GetProperty("SerializeData");
                var serializedData = serializedDataProperty.GetValue(cloth);

                var particleRadius = serializedData.GetType().GetField("radius");
                if (particleRadius != null)
                {
                    var particleRadiusData = particleRadius.GetValue(serializedData);
                    var particleRadiusValueField = particleRadiusData.GetType().GetField("value");
                    if (particleRadiusValueField != null)
                    {
                        particleRadiusValueField.SetValue(particleRadiusData, 0.01f); // set the particle radius -- helps avoid the collider pushing out the cloth
                    }
                }

                var collisionConstraint = serializedData.GetType().GetField("colliderCollisionConstraint");
                if (collisionConstraint != null)
                {
                    var collisionConstraintData = collisionConstraint.GetValue(serializedData);
                    if (collisionConstraintData != null)
                    {
                        var colliderListField = collisionConstraintData.GetType().GetField("colliderList");
                        if (colliderListField != null)
                        {
                            var actualColliderList = colliderListField.GetValue(collisionConstraintData);
                            if (actualColliderList != null)
                            {
                                colliderListField.SetValue(collisionConstraintData, magicaColliderList);
                            }
                        }
                    }
                }

                MethodInfo setParameterChange = cloth.GetType().GetMethod("SetParameterChange");
                setParameterChange.Invoke(cloth, new object[] { });
            }
        }

        private void DoMagicaCloth(Object cloth, GameObject obj, SoftPhysicsData data)
        {
            // needs a skinned mesh renderer, a weightmap and a list of magica colliders
            var serializedDataProperty = cloth.GetType().GetProperty("SerializeData");
            var serializedData = serializedDataProperty.GetValue(cloth);

            if (serializedData != null)
            {
                var sourceRenderersField = serializedData.GetType().GetField("sourceRenderers");
                if (sourceRenderersField != null)
                {
                    SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null)
                    {
                        var rendererList = sourceRenderersField.GetValue(serializedData);
                        List<Renderer> renderers;

                        if (rendererList == null)
                        {
                            renderers = new List<Renderer>();
                        }
                        else
                        {
                            renderers = (List<Renderer>)rendererList;
                        }

                        //if (!renderers.Contains(smr))
                        //{
                        renderers.Add(smr);
                        //}
                        sourceRenderersField.SetValue(serializedData, renderers);

                        var reductionSettingField = serializedData.GetType().GetField("reductionSetting");
                        if (reductionSettingField != null)
                        {
                            var reductionsettingFieldValue = reductionSettingField.GetValue(serializedData);
                            SetTypeField(reductionsettingFieldValue.GetType(), reductionsettingFieldValue, "simpleDistance", 0.06f);
                            SetTypeField(reductionsettingFieldValue.GetType(), reductionsettingFieldValue, "shapeDistance", 0.06f);
                        }

                        var paintModeField = serializedData.GetType().GetField("paintMode");
                        if (paintModeField != null)
                        {
                            paintModeField.SetValue(serializedData, 1); //MagicaCloth2.ClothSerializeData.PaintMode.Texture_Fixed_Move
                        }

                        var paintMapsField = serializedData.GetType().GetField("paintMaps");
                        if (paintMapsField != null)
                        {
                            List<Texture2D> paintMaps = new List<Texture2D>();

                            //Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                            Texture2D weightMap = ConvertWeightmap(data);
                            if (!weightMap) weightMap = Texture2D.blackTexture;
                            //paintMaps.Add((Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(simpleWeightmapPath));
                            paintMaps.Add(weightMap);

                            paintMapsField.SetValue(serializedData, paintMaps);
                        }

                        MethodInfo setParameterChange = cloth.GetType().GetMethod("SetParameterChange");
                        setParameterChange.Invoke(cloth, new object[] { });
                    }
                }
            }
        }

        private void AddMagicaBoneCloth()
        {
            // construct a single instance of magica BoneCloth
            // TODO: This section needs to be reorganized to deal with multiple spring bone systems requiring inidividual paramaters i.e. multiple bonecloth instances
            var cloth = AddMagicaClothInstance(1);

            Type clothType = GetTypeInAssemblies("MagicaCloth2.MagicaCloth");
            if (clothType != null)
            {
                IList magicaColliderList = FetchMagicaColliders(prefabInstance.gameObject, GetVaildSpringBoneColliders());

                var serializedDataProperty = cloth.GetType().GetProperty("SerializeData");
                var serializedData = serializedDataProperty.GetValue(cloth);

                List<GameObject> springRigs = new List<GameObject>();
                MeshUtil.FindCharacterBones(prefabInstance, springRigs, "RL_Hair_Rig_", "RLS_");

                List<Transform> hairRoots = new List<Transform>();
                // build all spring rigs
                foreach (GameObject rigBone in springRigs)
                {
                    Util.LogInfo("Processing Spring Bone Rig: " + rigBone.name);
                    for (int i = 0; i < rigBone.transform.childCount; i++)
                    {
                        Transform childBone = rigBone.transform.GetChild(i);
                        hairRoots.Add(childBone);
                    }
                }

                if (hairRoots.Count > 0)
                {
                    Util.LogInfo("Found: " + hairRoots.Count + " spring bone chains");

                    var rootBonesField = serializedData.GetType().GetField("rootBones");
                    if (rootBonesField != null)
                    {
                        //Debug.Log("Got Root Bones List");
                        //var rootBonesList = rootBonesField.GetValue(serializedData);
                        //if (rootBonesList != null)
                        //{
                        //    Debug.Log("Got Root Bones List Data");
                        //}
                        rootBonesField.SetValue(serializedData, hairRoots);
                    }

                    var collisionConstraint = serializedData.GetType().GetField("colliderCollisionConstraint");
                    if (collisionConstraint != null)
                    {
                        var collisionConstraintData = collisionConstraint.GetValue(serializedData);
                        if (collisionConstraintData != null)
                        {
                            var colliderListField = collisionConstraintData.GetType().GetField("colliderList");
                            if (colliderListField != null)
                            {
                                var actualColliderList = colliderListField.GetValue(collisionConstraintData);
                                if (actualColliderList != null)
                                {                                    
                                    colliderListField.SetValue(collisionConstraintData, magicaColliderList);
                                }
                            }
                        }
                    }
                }
                MethodInfo setParameterChange = cloth.GetType().GetMethod("SetParameterChange");
                setParameterChange.Invoke(cloth, new object[] { });
            }
        }

        private Object AddMagicaClothInstance(int typeValue)
        {
            // enum ClothProcess.ClothType MeshCloth = 0, BoneCloth = 1

            CharacterInfo currentChar = ImporterWindow.Current.Character;
            string fbxPath = currentChar.path;
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            if (importer != null)
            {
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }

            GameObject g = prefabInstance.gameObject;
            Type clothType = GetTypeInAssemblies("MagicaCloth2.MagicaCloth");
            if (clothType != null)
            {
                // add cloth component
                var existingCloth = g.GetComponent(clothType);
                if (existingCloth)
                {
                    if (GetMagicaClothType(existingCloth) == typeValue)
                    {
                        GameObject.DestroyImmediate(existingCloth); // if its an existing instance of same type then destroy it
                    }
                }

                var cloth = g.AddComponent(clothType);

                bool clothSet = SetMagicaClothType(cloth, typeValue);
                return cloth;
            }
            return null;
        }

        private int GetMagicaClothType(Object cloth)
        {
            // enum ClothProcess.ClothType MeshCloth = 0, BoneCloth = 1

            var serializedDataProperty = cloth.GetType().GetProperty("SerializeData");
            var serializedData = serializedDataProperty.GetValue(cloth);
            if (serializedData != null)
            {
                var clothTypeField = serializedData.GetType().GetField("clothType");
                if (clothTypeField != null)
                {
                    var clothTypeData = clothTypeField.GetValue(serializedData);

                    if (clothTypeData.ToString() == "MeshCloth") return 0;
                    if (clothTypeData.ToString() == "BoneCloth") return 1;
                }
            }
            return -1;
        }

        private bool SetMagicaClothType(Object cloth, int value)
        {
            // enum ClothProcess.ClothType MeshCloth = 0, BoneCloth = 1

            var serializedDataProperty = cloth.GetType().GetProperty("SerializeData");
            var serializedData = serializedDataProperty.GetValue(cloth);
            if (serializedData != null)
            {
                var clothTypeField = serializedData.GetType().GetField("clothType");
                if (clothTypeField != null)
                {
                    clothTypeField.SetValue(serializedData, value);
                    return true;
                }
            }
            return false;
        }

        public void RemoveCloth(GameObject obj)
        {
            Cloth cloth = obj.GetComponent<Cloth>();
            if (cloth) Component.DestroyImmediate(cloth);

            WeightMapper mapper = obj.GetComponent<WeightMapper>();
            if (mapper) Component.DestroyImmediate(mapper);

            Type magicaClothType = GetTypeInAssemblies("MagicaCloth2.MagicaCloth");
            if (magicaClothType != null)
            {
                var magicaClothInstance = obj.GetComponent(magicaClothType);
                if (magicaClothInstance != null) Component.DestroyImmediate(magicaClothInstance);
            }
        }

        private Texture2D GetTextureFrom(string jsonTexturePath, string materialName, string suffix, out string name, bool search)
        {
            Texture2D tex = null;
            name = "";

            // try to find the texture from the supplied texture path (usually from the json data).
            if (!string.IsNullOrEmpty(jsonTexturePath))
            {
                // try to load the texture asset directly from the json path.
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Util.CombineJsonTexPath(fbxFolder, jsonTexturePath));
                name = Path.GetFileNameWithoutExtension(jsonTexturePath);

                // if that fails, try to find the texture by name in the texture folders.
                if (!tex && search)
                {
                    tex = Util.FindTexture(textureFolders.ToArray(), name);
                }
            }

            // as a final fallback try to find the texture from the material name and suffix.
            if (!tex && search)
            {
                name = materialName + "_" + suffix;
                tex = Util.FindTexture(textureFolders.ToArray(), name);
            }

            return tex;
        }

        private void SetWeightMapImport(Texture2D tex)
        {
            if (!tex) return;

            // now fix the import settings for the texture.
            string path = AssetDatabase.GetAssetPath(tex);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 0;
            bool magica = characterInfo.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaCloth);
            importer.maxTextureSize = magica ? MAGICA_WEIGHT_SIZE : 2048;

            AssetDatabase.WriteImportSettingsIfDirty(path);
        }

        public static GameObject RebuildPhysics(CharacterInfo characterInfo)
        {
            GameObject prefabAsset = characterInfo.PrefabAsset;

            if (prefabAsset)
            {
                GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

                if (prefabAsset && prefabInstance && characterInfo.PhysicsJsonData != null)
                {
                    //characterInfo.ShaderFlags |= CharacterInfo.ShaderFeatureFlags.ClothPhysics;
                    Physics physics = new Physics(characterInfo, prefabInstance);
                    physics.AddPhysics(true);
                    characterInfo.Write();
                }

                if (prefabInstance) GameObject.DestroyImmediate(prefabInstance);
            }

            return prefabAsset;
        }

        void FixColliderAPose(Transform[] objects, Dictionary<Object, string> colliderLookup)
        {
            Func<string, Transform> FindBone = (boneName) => Array.Find(objects, o => o.name.iEquals(boneName));

            Transform leftArm = FindBone("CC_Base_L_Upperarm");
            if (!leftArm) leftArm = FindBone("L_Upperarm");

            Transform rightArm = FindBone("CC_Base_R_Upperarm");
            if (!leftArm) rightArm = FindBone("R_Upperarm");

            foreach (KeyValuePair<Object, string> pair in colliderLookup)
            {
                Component c = pair.Key as Component;
                if (c)
                {
                    Transform t = c.transform;
                    string boneName = pair.Value;
                    Transform bone = FindBone(boneName);
                    if (bone)
                    {
                        if (bone == leftArm || bone.IsChildOf(leftArm))
                        {
                            t.RotateAround(leftArm.position, Vector3.forward, -30f);
                        }
                        else if (bone == rightArm || bone.IsChildOf(rightArm))
                        {
                            t.RotateAround(rightArm.position, Vector3.forward, 30f);
                        }
                    }
                }

            }
        }

        public static System.Type GetTypeInAssemblies(string typeName)
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assemblies)
            {
                System.Type[] types = a.GetTypes();
                foreach (System.Type t in types)
                {
                    if (typeName == t.FullName)
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        public static bool GetTypeField(object o, string field, out object value)
        {
            FieldInfo fieldInfo = o.GetType().GetField(field);
            if (fieldInfo != null)
            {
                value = fieldInfo.GetValue(o);
                return true;
            }
            value = null;
            return false;
        }

        public static bool SetTypeField(Type t, object o, string field, object value)
        {
            FieldInfo fRoots = t.GetField(field);
            if (fRoots != null)
            {
                fRoots.SetValue(o, value);
                return true;
            }
            return false;
        }

        public static bool GetTypeProperty(object o, string property, out object value)
        {
            PropertyInfo propertyInfo = o.GetType().GetProperty(property);
            if (propertyInfo != null)
            {
                value = propertyInfo.GetValue(o);
                return true;
            }
            value = null;
            return false;
        }

        public static bool SetTypeProperty(object o, string property, object value)
        {
            PropertyInfo propertyInfo = o.GetType().GetProperty(property);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(o, value);
                return true;
            }
            return false;
        }

        // additions        
        public static bool CreateAbstractColliders(ColliderManager colliderManager, out List<ColliderManager.AbstractCapsuleCollider> abstractColliders)
        {
            CharacterInfo current;

            if (ImporterWindow.Current != null)
            {
                // current live info (used for shaderflags) allows for switching between native and magica and rebuilding physics
                current = ImporterWindow.Current.Character;  
            }
            else
            {
                // contains shaderflags from last build - this is acceptable when this function is called from the collidermanager in the absence of an importer window
                current = new CharacterInfo(colliderManager.characterGUID); 
            }
            
            bool native = current.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.UnityClothPhysics);
            bool nativeHair = current.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.UnityClothHairPhysics);
            bool magica = current.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaCloth) && MagicaCloth2IsAvailable();
            bool magicaBoneHair = current.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaBone) && MagicaCloth2IsAvailable();
            bool dynamic = current.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.SpringBoneHair) && DynamicBoneIsAvailable();
            abstractColliders = new List<ColliderManager.AbstractCapsuleCollider>();

            if (MagicaCloth2IsAvailable())
            {
                colliderManager.magicaColliderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider"); // very slow
                //Debug.Log("CreateAbstractColliders: Magica Cloth Available - Magica Determination is set to: " + magica.ToString());
                //Debug.Log("current magica shaderflags == " + current.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.MagicaCloth).ToString());
            }

            if (DynamicBoneIsAvailable())
            {
                colliderManager.dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneCollider"); // very slow
                //Debug.Log("CreateAbstractColliders: DynamicBone Available - Dynamic Bone Determination is set to: " + dynamic.ToString());
                //Debug.Log("current dynamicbone shaderflags == " + current.ShaderFlags.HasFlag(CharacterInfo.ShaderFeatureFlags.SpringBoneHair).ToString());
            }
            // create an array of the in-scene transforms in the character hierarchy
            Transform[] allChildTransforms = colliderManager.gameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform childtransform in allChildTransforms)
            {
                GameObject go = childtransform.gameObject;
                if (colliderManager.TransformHasAnyValidCollider(childtransform))
                {
                    ColliderManager.AbstractCapsuleCollider abs = new ColliderManager.AbstractCapsuleCollider();

                    if (native || nativeHair)
                    {
                        if (go.GetComponent(typeof(CapsuleCollider)))
                        {
                            //Debug.Log("Found Native Capsule Collider: " + go.name);
                            CapsuleCollider coll = go.GetComponent<CapsuleCollider>();
                            abs.transform = coll.transform;
                            abs.isEnabled = coll.transform.gameObject.activeSelf;
                            abs.localPosition = coll.transform.localPosition;
                            abs.localRotation = coll.transform.localRotation;
                            abs.height = coll.height;
                            abs.radius = coll.radius;
                            abs.name = coll.name;
                            abs.axis = (ColliderManager.ColliderAxis)coll.direction;
                            abs.nativeRef = coll;
                            abs.colliderTypes |= ColliderManager.ColliderType.UnityEngine;
                        }
                    }

                    if (magica || magicaBoneHair)
                    {
                        if (colliderManager.magicaColliderType == null)
                            colliderManager.magicaColliderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");
                                                
                        var magicaColl = go.GetComponent(colliderManager.magicaColliderType);
                        if (magicaColl != null)
                        {
                            //Debug.Log("FOUND Magica Capsule Collider: " + go.name);
                            if (colliderManager.magicaGetSize == null)
                                colliderManager.magicaGetSize = magicaColl.GetType().GetMethod("GetSize");

                            if (abs.colliderTypes.HasFlag(ColliderManager.ColliderType.UnityEngine))
                            {
                                abs.magicaRef = magicaColl;
                            }
                            else
                            {
                                abs.transform = go.transform;
                                abs.isEnabled = go.activeSelf;
                                abs.localPosition = go.transform.localPosition;
                                abs.localRotation = go.transform.localRotation;

                                // see: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.invoke?view=net-7.0
                                Vector3 size = (Vector3)colliderManager.magicaGetSize.Invoke(magicaColl, new object[] { });
                                abs.height = size.z;
                                abs.radius = size.x;

                                if (GetTypeProperty(magicaColl, "name", out object _name))
                                    abs.name = (string)_name;

                                if (GetTypeField(magicaColl, "direction", out object _axis))
                                    abs.axis = (ColliderManager.ColliderAxis)_axis;

                                abs.magicaRef = magicaColl;
                            }
                            abs.colliderTypes |= ColliderManager.ColliderType.MagicaCloth2;
                        }
                    }
                    if (dynamic)
                    {                        
                        if (colliderManager.dynamicBoneColliderType == null)
                            colliderManager.dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneCollider");

                        var dynamicColl = go.GetComponent(colliderManager.dynamicBoneColliderType);
                        if( dynamicColl != null)
                        {
                            if (abs.colliderTypes.HasFlag(ColliderManager.ColliderType.UnityEngine) || abs.colliderTypes.HasFlag(ColliderManager.ColliderType.MagicaCloth2))
                            {
                                abs.dynamicRef = dynamicColl;
                            }
                            else
                            {
                                abs.transform = go.transform;
                                abs.isEnabled = go.activeSelf;
                                abs.localPosition = go.transform.localPosition;
                                abs.localRotation = go.transform.localRotation;

                                GetTypeField(dynamicColl, "m_Height", out object _height);
                                abs.height = (float)_height;

                                GetTypeField(dynamicColl, "m_Radius", out object _radius);
                                abs.radius = (float)_radius;

                                if (GetTypeProperty(dynamicColl, "name", out object _name))
                                    abs.name = (string)_name;

                                if (GetTypeField(dynamicColl, "m_Direction", out object _axis))
                                    abs.axis = (ColliderManager.ColliderAxis)_axis;

                                abs.dynamicRef = dynamicColl;
                            }
                            abs.colliderTypes |= ColliderManager.ColliderType.DynamicBone;
                        }
                    }
                    if (!ColliderManager.AbstractCapsuleCollider.IsNullOrEmpty(abs))
                    {
                        abstractColliders.Add(abs);
                    }
                }
            }
            if (abstractColliders.Count > 0)
                return true;
            else
                return false;
        }

        public static bool MagicaCloth2IsAvailable()
        {
            // basic magica cloth v2 test -- very slow
            Type magicaClothType = GetTypeInAssemblies("MagicaCloth2.MagicaCloth");
            return magicaClothType != null;
        }

        public static bool DynamicBoneIsAvailable()
        {
            // basic dynamic bone test -- very slow
            Type dynamicBoneType = GetTypeInAssemblies("DynamicBone");
            return dynamicBoneType != null;
        }

        public Object AddDynamicBoneCollider(GameObject g, CollisionShapeData collider)
        {
            Type dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneCollider");
            if (dynamicBoneColliderType != null)
            {
                var dynamicBoneColliderComponent = g.AddComponent(dynamicBoneColliderType);

                float r = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                float m_Radius = 0f;
                float m_Height = 0f;
                Vector3 m_Center = Vector3.zero;
                int m_Direction = (int)collider.colliderAxis;

                if (collider.colliderType == ColliderType.Sphere)
                {
                    m_Radius = r;
                    m_Height = 0f;
                }
                else if (collider.colliderType == ColliderType.Capsule)
                {
                    m_Radius = r;
                    m_Height = collider.length * modelScale + r * 2f;
                }
                else
                {
                    float radius;
                    float height;
                    switch (collider.colliderAxis)
                    {
                        case ColliderAxis.X:
                            radius = (collider.extent.y + collider.extent.z) / 4f;
                            height = collider.extent.x;
                            break;
                        case ColliderAxis.Z:
                            radius = (collider.extent.x + collider.extent.y) / 4f;
                            height = collider.extent.z;
                            break;
                        case ColliderAxis.Y:
                        default:
                            radius = (collider.extent.x + collider.extent.z) / 4f;
                            height = collider.extent.y;
                            break;
                    }
                    m_Radius = radius;
                    m_Height = height;
                }

                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Radius", m_Radius);
                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Height", m_Height);               
                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Center", m_Center);
                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Direction", m_Direction);

                return dynamicBoneColliderComponent;
            }
            return null;
        }

        public Object AddMagicaCloth2Collider(GameObject g, CollisionShapeData collider)
        {
            Type capsuleColliderType = Physics.GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");
            if (capsuleColliderType != null)
            {
                var capsuleColliderComponent = g.AddComponent(capsuleColliderType);

                Physics.SetTypeField(capsuleColliderComponent.GetType(), capsuleColliderComponent, "direction", (int)collider.colliderAxis);
                Physics.SetTypeField(capsuleColliderComponent.GetType(), capsuleColliderComponent, "radiusSeparation", false);

                float r;
                float h;
                if (collider.colliderType.Equals(ColliderType.Capsule))
                {
                    r = (collider.radius - collider.margin * Physics.PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    h = collider.length * modelScale + r * 2f;
                }
                else if (collider.colliderType.Equals(ColliderType.Sphere))
                {
                    float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    r = radius;
                    h = 0f;
                }
                else
                {
                    float radius;
                    float height;
                    switch (collider.colliderAxis)
                    {
                        case ColliderAxis.X:
                            radius = (collider.extent.y + collider.extent.z) / 4f;
                            height = collider.extent.x;
                            break;
                        case ColliderAxis.Z:
                            radius = (collider.extent.x + collider.extent.y) / 4f;
                            height = collider.extent.z;
                            break;
                        case ColliderAxis.Y:
                        default:
                            radius = (collider.extent.x + collider.extent.z) / 4f;
                            height = collider.extent.y;
                            break;
                    }
                    r = (radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    h = height * modelScale;
                }

                //"https://learn.microsoft.com/en-us/dotnet/api/system.type.getmethod?view=netframework-4.8#System_Type_GetMethod_System_String_System_Type___"
                MethodInfo setSize = capsuleColliderComponent.GetType().GetMethod("SetSize",
                                    BindingFlags.Public | BindingFlags.Instance,
                                    null,
                                    CallingConventions.Any,
                                    new Type[] { typeof(Vector3) },
                                    null);
                Vector3 sizeVector = new Vector3(r, r, h);
                object[] inputParams = new object[] { sizeVector };
                setSize.Invoke(capsuleColliderComponent, inputParams);

                MethodInfo update = capsuleColliderComponent.GetType().GetMethod("UpdateParameters");
                update.Invoke(capsuleColliderComponent, new object[] { });

                return capsuleColliderComponent;
            }
            return null;
        }

        public Object AddNativeCollider(GameObject g, CollisionShapeData collider)
        {
            if (collider.colliderType.Equals(ColliderType.Capsule))
            {
                CapsuleCollider c = g.AddComponent<CapsuleCollider>();

                c.direction = (int)collider.colliderAxis;
                float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                c.radius = radius;
                c.height = collider.length * modelScale + radius * 2f;
                return c;
            }
            else if (collider.colliderType.Equals(ColliderType.Sphere))
            {
                CapsuleCollider c = g.AddComponent<CapsuleCollider>();

                c.direction = (int)collider.colliderAxis;
                float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                c.radius = radius;
                c.height = 0f;
                return c;
            }
            else
            {
                CapsuleCollider c = g.AddComponent<CapsuleCollider>();
                c.direction = (int)collider.colliderAxis;
                float radius;
                float height;
                switch (collider.colliderAxis)
                {
                    case ColliderAxis.X:
                        radius = (collider.extent.y + collider.extent.z) / 4f;
                        height = collider.extent.x;
                        break;
                    case ColliderAxis.Z:
                        radius = (collider.extent.x + collider.extent.y) / 4f;
                        height = collider.extent.z;
                        break;
                    case ColliderAxis.Y:
                    default:
                        radius = (collider.extent.x + collider.extent.z) / 4f;
                        height = collider.extent.y;
                        break;
                }
                c.radius = (radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                c.height = height * modelScale;
                return c;
            }
        }

        public static bool HasNativeCapsuleColliders(GameObject go)
        {
            return go.GetComponentsInChildren(typeof(CapsuleCollider)).Length > 0;
        }

        public static bool HasMagicaCloth2Colliders(GameObject go)
        {
            Type magicaClothType = GetTypeInAssemblies("MagicaCloth2.MagicaCloth");
            if (magicaClothType != null)
            {
                Type magicaCollderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");
                if (magicaCollderType != null)
                {
                    Component[] components = go.GetComponentsInChildren(magicaCollderType);
                    return components.Length > 0;
                }
            }
            return false;
        }

        public static bool HasDynamicBoneColliders(GameObject go)
        {
            Type dynamicBoneType = GetTypeInAssemblies("DynamicBone");

            //

            return false;
        }

        public static void ReadAllTypeProperties(object o)
        {
            PropertyInfo[] propertyInfo = o.GetType().GetProperties();
            foreach (PropertyInfo property in propertyInfo)
            {
                Debug.Log("PROPERTY:: " + property.Name);
            }
        }

        public static void ReadAllTypeFields(object o)
        {
            FieldInfo[] fieldInfo = o.GetType().GetFields();
            foreach (FieldInfo field in fieldInfo)
            {
                Debug.Log("FIELD:: " + field.Name);
            }
        }

        //temp solution
        private Texture2D ConvertWeightmap(SoftPhysicsData data)
        {
            Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
            if (!weightMap.isReadable) return null;

            Texture2D lowOutputMap;
            bool useCompute = true;

            if (useCompute)
            {
                CharacterInfo currentCharacter = ImporterWindow.Current.Character;

                string[] folders = new string[] { "Assets", "Packages" };
                Texture2D physXWeightMap = Util.FindTexture(folders, "physXWeightMapTest");
                string folder = ComputeBake.BakeTexturesFolder(currentCharacter.path);
                string name = Path.GetFileNameWithoutExtension(data.weightMapPath) + "_" + MAGICA_WEIGHT_SIZE + "_magica";// "magicaWeightMapTest";
                float threshold = 0f; // 1f / 255f;
                Vector2Int size = new Vector2Int(MAGICA_WEIGHT_SIZE, MAGICA_WEIGHT_SIZE);
                // should create the texture in: <current character folder>/Baked/<character name>/Textures
                lowOutputMap = ComputeBake.BakeMagicaWeightMap(weightMap, threshold, size, folder, name);
                SetWeightMapImport(lowOutputMap);
            }
            else
            {
                int sampleX = (int)Mathf.Floor(weightMap.width / 64);
                int sampleY = (int)Mathf.Floor(weightMap.height / 64);

                lowOutputMap = new Texture2D(64, 64);
                for (int i = 0; i < 64; i++)
                {
                    for (int j = 0; j < 64; j++)
                    {
                        Color sample = weightMap.GetPixel(i * sampleX, j * sampleY);
                        if (sample.g > 0.2f)
                        {
                            lowOutputMap.SetPixel(i, j, new Color(0f, 1f, 0f));
                        }
                        else
                        {
                            lowOutputMap.SetPixel(i, j, new Color(1f, 0f, 0f));
                        }
                    }
                }
                lowOutputMap.Apply();
                string assetPath = AssetDatabase.GetAssetPath(weightMap);
                string assetDir = Path.GetDirectoryName(assetPath);
                string assetExt = Path.GetExtension(assetPath);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);

                string outputName = assetName + "_MAGICA";
                string outPutPath = assetDir + "/" + outputName + ".asset";
                AssetDatabase.CreateAsset(lowOutputMap, outPutPath);
                AssetDatabase.SaveAssets();
            }
            return lowOutputMap;
        }

        public IList FetchMagicaColliders(GameObject prefabObject, List<string> matchingBoneList = null)
        {
            if (!MagicaCloth2IsAvailable()) return null;

            //var magicaColliderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");
            var magicaColliderType = GetTypeInAssemblies("MagicaCloth2.ColliderComponent");
            IList genericColliders = (IList)CreateGeneric(typeof(List<>), magicaColliderType);

            Transform[] allChildTransforms = prefabObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform childtransform in allChildTransforms)
            {
                GameObject go = childtransform.gameObject;
                var magicaColl = go.GetComponent(magicaColliderType);
                if (magicaColl != null)
                {
                    if (matchingBoneList == null) // all magica colliders added to list
                    {
                        genericColliders.Add(magicaColl);
                    }
                    else if (matchingBoneList.Contains(childtransform.parent.name)) // only magica colliders on the matching bone added to list
                    {
                        genericColliders.Add(magicaColl);                        
                    }
                }
            }
            return genericColliders;
        }

        public IList FetchDynamicBoneColliders(GameObject prefabObject, List<string> matchingBoneList = null)
        {
            if (!MagicaCloth2IsAvailable()) return null;

            var dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneColliderBase");
            IList genericColliders = (IList)CreateGeneric(typeof(List<>), dynamicBoneColliderType);

            Transform[] allChildTransforms = prefabObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform childtransform in allChildTransforms)
            {
                GameObject go = childtransform.gameObject;
                var dynBoneColl = go.GetComponent(dynamicBoneColliderType);
                if (dynBoneColl != null)
                {
                    if (matchingBoneList == null) // all dynamic bone colliders added to list
                    {
                        genericColliders.Add(dynBoneColl);
                    }
                    else if (matchingBoneList.Contains(childtransform.parent.name)) // only dynamic bone colliders on the matching bone added to list
                    {
                        genericColliders.Add(dynBoneColl);
                    }
                }
            }
            return genericColliders;
        }

        public bool TryGetExistingColliderGameObject(string colliderName, Transform[] prefabObjects, out GameObject colliderGameObject)
        {
            foreach (Transform childtransform in prefabObjects)
            {
                if (childtransform.name == colliderName)
                {
                    colliderGameObject = childtransform.gameObject;

                    if (MAGICA_CLOTH_AVAILABLE)
                    {
                        var magicaColliderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");
                        if (magicaColliderType != null)
                        {
                            var mCol = colliderGameObject.GetComponent(magicaColliderType);
                            if (mCol)
                            {
                                GameObject.DestroyImmediate(mCol);
                            }
                        }
                    }

                    var col = colliderGameObject.GetComponent<Collider>();
                    if (col != null)
                    {
                        GameObject.DestroyImmediate(col);
                    }

                    return true;
                }
            }

            colliderGameObject = new GameObject(colliderName);
            return false;
        }

        // =================
        // =================
        // Legacy functions
        // =================
        // =================
        #region Legacy Functions

        private void AddColliders()
        {
            if (!addClothPhysics && !addHairPhysics && !addHairSpringBones)
            {
                ColliderManager existingColliderManager = prefabInstance.GetComponent<ColliderManager>();
                if (existingColliderManager != null)
                {
                    foreach (Collider c in existingColliderManager.colliders)
                    {
                        GameObject.DestroyImmediate(c.gameObject);
                    }
                    Component.DestroyImmediate(existingColliderManager);
                }
                return;
            }

            MAGICA_CLOTH_AVAILABLE = MagicaCloth2IsAvailable();

            GameObject parent = new GameObject();
            GameObject g;
            Transform[] objects = prefabInstance.GetComponentsInChildren<Transform>();
            Dictionary<Object, string> colliderLookup = new Dictionary<Object, string>();
            Dictionary<Collider, Collider> existingLookup = new Dictionary<Collider, Collider>();

            // delegates
            Func<string, Transform> FindBone = (boneName) => Array.Find(objects, o => o.name.Equals(boneName));
            Func<string, Transform, Collider> FindColliderObj = (colliderName, bone) =>
            {
                for (int i = 0; i < bone.childCount; i++)
                {
                    Transform child = bone.GetChild(i);
                    if (child.name == colliderName)
                    {
                        return child.GetComponent<Collider>();
                    }
                }
                return null;
            };

            //bool objectExists = false;
            foreach (CollisionShapeData collider in boneColliders)
            {
                string colliderName = collider.boneName + "_" + collider.name;
                Transform bone = FindBone(collider.boneName);
                Collider existingCollider = FindColliderObj(colliderName, bone);

                /*
                // addition
                objectExists = TryGetExistingColliderGameObject(colliderName, objects, out g);
                
                if (!objectExists)
                {
                    g.transform.SetParent(parent.transform);

                    Transform t = g.transform;
                    t.position = collider.translation * modelScale;
                    t.rotation = collider.rotation;

                    AddCollider(g, collider, colliderLookup);
                }
                else
                {                    
                    // longhand test of changing hierarchy inside a prefab
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(ImporterWindow.Current.Character.PrefabAsset)))
                    {
                        var prefabRoot = editingScope.prefabContentsRoot;
                        Transform[] editablePrefabTransforms = prefabRoot.GetComponentsInChildren<Transform>();
                        foreach (Transform child in editablePrefabTransforms)
                        {
                            if (child.name == colliderName)
                            {
                                child.SetParent(parent.transform);
                                child.position = collider.translation * modelScale;
                                child.rotation = collider.rotation;
                                g = child.gameObject;
                                AddCollider(g, collider, colliderLookup);
                                break;
                            }
                        }
                    }
                    
                }
                //
                */

                g = new GameObject();
                g.transform.SetParent(parent.transform);
                g.name = colliderName;


                Transform t = g.transform;
                t.position = collider.translation * modelScale;
                t.rotation = collider.rotation;


                //original add colliders


                if (collider.colliderType.Equals(ColliderType.Capsule))
                {
                    if (addMagicaClothPhysics)
                    {
                        if (MagicaCloth2IsAvailable())
                        {
                            Object c = AddMagicaCloth2Collider(g, collider);
                            colliderLookup.Add(c, collider.boneName);
                        }
                    }

                    if (addUnityClothPhysics)
                    {
                        CapsuleCollider c = g.AddComponent<CapsuleCollider>();

                        c.direction = (int)collider.colliderAxis;
                        float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                        c.radius = radius;
                        c.height = collider.length * modelScale + radius * 2f;
                        colliderLookup.Add(c, collider.boneName);
                        if (existingCollider) existingLookup.Add(c, existingCollider);
                    }
                }
                else if (collider.colliderType.Equals(ColliderType.Sphere))
                {
                    if (addMagicaClothPhysics)
                    {
                        if (MagicaCloth2IsAvailable())
                        {
                            Object c = AddMagicaCloth2Collider(g, collider);
                            colliderLookup.Add(c, collider.boneName);
                        }
                    }

                    if (addUnityClothPhysics)
                    {
                        CapsuleCollider c = g.AddComponent<CapsuleCollider>();

                        c.direction = (int)collider.colliderAxis;
                        float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                        c.radius = radius;
                        c.height = 0f;
                        colliderLookup.Add(c, collider.boneName);
                        if (existingCollider) existingLookup.Add(c, existingCollider);
                    }
                }
                else
                {
                    //BoxCollider b = g.gameObject.AddComponent<BoxCollider>();
                    //b.size = collider.extent * modelScale;
                    //colliderLookup.Add(b, collider.boneName);

                    if (addMagicaClothPhysics)
                    {
                        if (MagicaCloth2IsAvailable() && addMagicaClothPhysics)
                        {
                            Object c = AddMagicaCloth2Collider(g, collider);
                            colliderLookup.Add(c, collider.boneName);
                        }
                    }

                    if (addUnityClothPhysics)
                    {
                        CapsuleCollider c = g.AddComponent<CapsuleCollider>();
                        c.direction = (int)collider.colliderAxis;
                        float radius;
                        float height;
                        switch (collider.colliderAxis)
                        {
                            case ColliderAxis.X:
                                radius = (collider.extent.y + collider.extent.z) / 4f;
                                height = collider.extent.x;
                                break;
                            case ColliderAxis.Z:
                                radius = (collider.extent.x + collider.extent.y) / 4f;
                                height = collider.extent.z;
                                break;
                            case ColliderAxis.Y:
                            default:
                                radius = (collider.extent.x + collider.extent.z) / 4f;
                                height = collider.extent.y;
                                break;
                        }
                        c.radius = (radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                        c.height = height * modelScale;
                        colliderLookup.Add(c, collider.boneName);
                        if (existingCollider) existingLookup.Add(c, existingCollider);
                    }
                }


                //end of original add colliders
            }

            //if (!objectExists)
            //{
            parent.transform.Rotate(Vector3.left, 90);
            parent.transform.localScale = new Vector3(-1f, 1f, 1f);
            //}

            if (aPose) FixColliderAPose(objects, colliderLookup);

            // as the transforms have moved, need to re-sync the transforms in the physics engine
            UnityEngine.Physics.SyncTransforms();

            List<Object> listColliders = new List<Object>(colliderLookup.Count);

            // revert all existing prefabs overrides...
            foreach (KeyValuePair<Collider, Collider> collPair in existingLookup)
            {
                PrefabUtility.RevertObjectOverride(collPair.Value, InteractionMode.UserAction);
            }

            foreach (KeyValuePair<Object, string> collPair in colliderLookup)
            {
                Component c = collPair.Key as Component;
                if (c)
                {
                    Transform t = c.transform;

                    string colliderBone = collPair.Value;

                    Transform bone = FindBone(colliderBone);
                    if (bone)
                    {
                        /* dunno aboot this...
                        if (existingLookup.TryGetValue(transformedCollider, out Collider existingCollider))
                        {
                            // reparent with keep position
                            transformedCollider.transform.SetParent(bone, true);
                            // copy the transformed collider to the existing if they match
                            CopyCollider(transformedCollider, existingCollider);
                            // delete the transformed collider
                            GameObject.DestroyImmediate(transformedCollider.gameObject);
                            // add to list of colliders
                            listColliders.Add(existingCollider);
                        }
                        else
                        {
                            // reparent with keep position
                            t.transform.SetParent(bone, true);
                            // add to list of colliders
                            listColliders.Add(transformedCollider);
                        }*/
                    }

                    // reparent with keep position
                    t.transform.SetParent(bone, true);
                    // add to list of colliders
                    listColliders.Add(t);
                }
            }

            // add collider manager to prefab root
            ColliderManager colliderManager = prefabInstance.GetComponent<ColliderManager>();
            if (colliderManager == null) colliderManager = prefabInstance.AddComponent<ColliderManager>();

            // add colliders to manager
            if (colliderManager)
            {
                colliderManager.characterGUID = characterGUID;
                colliderManager.AddColliders(listColliders);
            }
            
            // addition
            // create a reference list of abstract colliders here
            // to be used as a 'reset to defaults' resource
            CreateAbstractColliders(colliderManager, out List<ColliderManager.AbstractCapsuleCollider> abstractColliders);//, out IList genericColliders);
            Debug.Log("Build Time abstract count = " + abstractColliders.Count);
            PhysicsSettingsStore.SaveAbstractColliderSettings(colliderManager, abstractColliders, true);
            // end of addition

            Type dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneCollider");

            if (addHairSpringBones)
            {
                if (dynamicBoneColliderType == null)
                {
                    Debug.LogWarning("Warning: DynamicBone not found in project assembly.");
                }
                else
                {
                    foreach (CollisionShapeData collider in boneColliders)
                    {
                        if (springColliderBones.Contains(collider.boneName))
                        {
                            string colliderName = collider.boneName + "_" + collider.name;
                            Transform bone = FindBone(collider.boneName);
                            Collider existingCollider = FindColliderObj(colliderName, bone);

                            if (existingCollider && existingCollider.GetType() == typeof(CapsuleCollider))
                            {
                                CapsuleCollider cc = (CapsuleCollider)existingCollider;

                                var dynamicBoneColliderComponent = existingCollider.gameObject.GetComponent(dynamicBoneColliderType);
                                if (dynamicBoneColliderComponent == null)
                                {
                                    dynamicBoneColliderComponent = existingCollider.gameObject.AddComponent(dynamicBoneColliderType);
                                }

                                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Height", cc.height);
                                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Radius", cc.radius);
                                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Center", cc.center);
                                SetTypeField(dynamicBoneColliderType, dynamicBoneColliderComponent, "m_Direction", cc.direction);
                            }
                        }
                    }
                }
            }

            GameObject.DestroyImmediate(parent);
        }

        private void AddCollider(GameObject g, CollisionShapeData collider, Dictionary<Object, string> colliderLookup)
        {
            if (collider.colliderType.Equals(ColliderType.Capsule))
            {
                if (addMagicaClothPhysics)
                {
                    if (MagicaCloth2IsAvailable())
                    {
                        Object c = AddMagicaCloth2Collider(g, collider);
                        colliderLookup.Add(c, collider.boneName);
                    }
                }

                if (addUnityClothPhysics)
                {
                    CapsuleCollider c = g.AddComponent<CapsuleCollider>();

                    c.direction = (int)collider.colliderAxis;
                    float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    c.radius = radius;
                    c.height = collider.length * modelScale + radius * 2f;
                    colliderLookup.Add(c, collider.boneName);
                    //if (existingCollider) existingLookup.Add(c, existingCollider);
                }
            }
            else if (collider.colliderType.Equals(ColliderType.Sphere))
            {
                if (addMagicaClothPhysics)
                {
                    if (MagicaCloth2IsAvailable())
                    {
                        Object c = AddMagicaCloth2Collider(g, collider);
                        colliderLookup.Add(c, collider.boneName);
                    }
                }

                if (addUnityClothPhysics)
                {
                    CapsuleCollider c = g.AddComponent<CapsuleCollider>();

                    c.direction = (int)collider.colliderAxis;
                    float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    c.radius = radius;
                    c.height = 0f;
                    colliderLookup.Add(c, collider.boneName);
                    //if (existingCollider) existingLookup.Add(c, existingCollider);
                }
            }
            else
            {
                //BoxCollider b = g.gameObject.AddComponent<BoxCollider>();
                //b.size = collider.extent * modelScale;
                //colliderLookup.Add(b, collider.boneName);

                if (addMagicaClothPhysics)
                {
                    if (MagicaCloth2IsAvailable() && addMagicaClothPhysics)
                    {
                        Object c = AddMagicaCloth2Collider(g, collider);
                        colliderLookup.Add(c, collider.boneName);
                    }
                }

                if (addUnityClothPhysics)
                {
                    CapsuleCollider c = g.AddComponent<CapsuleCollider>();
                    c.direction = (int)collider.colliderAxis;
                    float radius;
                    float height;
                    switch (collider.colliderAxis)
                    {
                        case ColliderAxis.X:
                            radius = (collider.extent.y + collider.extent.z) / 4f;
                            height = collider.extent.x;
                            break;
                        case ColliderAxis.Z:
                            radius = (collider.extent.x + collider.extent.y) / 4f;
                            height = collider.extent.z;
                            break;
                        case ColliderAxis.Y:
                        default:
                            radius = (collider.extent.x + collider.extent.z) / 4f;
                            height = collider.extent.y;
                            break;
                    }
                    c.radius = (radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    c.height = height * modelScale;
                    colliderLookup.Add(c, collider.boneName);
                    //if (existingCollider) existingLookup.Add(c, existingCollider);
                }
            }
        }

        private bool CopyCollider(Collider from, Collider to)
        {
            if (from.GetType() != to.GetType()) return false;

            if (from.GetType() == typeof(CapsuleCollider))
            {
                CapsuleCollider ccFrom = (CapsuleCollider)from;
                CapsuleCollider ccTo = (CapsuleCollider)to;
                ccTo.direction = ccFrom.direction;
                ccTo.radius = ccFrom.radius;
                ccTo.height = ccFrom.height;
                ccTo.center = ccFrom.center;
                ccTo.transform.SetPositionAndRotation(ccFrom.transform.position, ccFrom.transform.rotation);
                ccTo.transform.localScale = ccFrom.transform.localScale;
                ccTo.enabled = ccFrom.enabled;
                return true;
            }
            else if (from.GetType() == typeof(SphereCollider))
            {
                SphereCollider ccFrom = (SphereCollider)from;
                SphereCollider ccTo = (SphereCollider)to;
                ccTo.radius = ccFrom.radius;
                ccTo.center = ccFrom.center;
                ccTo.transform.SetPositionAndRotation(ccFrom.transform.position, ccFrom.transform.rotation);
                ccTo.transform.localScale = ccFrom.transform.localScale;
                ccTo.enabled = ccFrom.enabled;
                return true;
            }

            return false;
        }
        #endregion Legacy Functions
    }
}
