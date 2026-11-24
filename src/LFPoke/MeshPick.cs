using System.Collections.Generic;
using UnityEngine;

namespace LFPoke
{
    /// <summary>What part of the character a click hit.</summary>
    internal enum BodyPart { None, Face, Head, BreastLeft, BreastRight, Foot, Tummy, ButtLeft, ButtRight, Leg, Eye, Mouth, Glasses }

    /// <summary>
    /// Uses the character's own 3D model as the hitbox: on a click the skinned meshes are baked in
    /// their current pose, the mouse ray is tested against their triangles, and the bones that move
    /// the hit triangle tell which body part it is. Works in every camera view and pose.
    /// </summary>
    internal static class MeshPick
    {
        internal struct Hit
        {
            public BodyPart part;
            public float distance;
            public Vector3 point;
            public string bone, renderer;
            public Vector3 normal;   // surface direction at the hit, facing the camera
        }

        private static readonly List<Vector3> _verts = new List<Vector3>();
        private static readonly List<BoneWeight> _weights = new List<BoneWeight>();

        internal static string LastDiag = "";
        internal static float BreastShare = 0.3f;   // breast bones must move the spot at least this much

        /// <summary>The triangle's normal, turned toward the camera (winding doesn't matter).</summary>
        private static Vector3 FacingNormal(Ray ray, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            return Vector3.Dot(n, ray.direction) > 0f ? -n : n;
        }

        internal static bool Pick(Giantess g, Ray ray, out Hit hit)
        {
            int nSmr = 0, nBounds = 0, nTris = 0;
            var names = new System.Text.StringBuilder();
            hit = default;
            hit.distance = float.MaxValue;
            if (g == null) return false;
            var anim = g.animator;
            foreach (var smr in g.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr == null || !smr.enabled || !smr.gameObject.activeInHierarchy || smr.sharedMesh == null) continue;
                nSmr++;
                bool inBounds = smr.bounds.IntersectRay(ray);   // diagnostics only: bounds can be stale
                if (inBounds) nBounds++;
                names.Append(smr.name).Append(inBounds ? "+ " : "- ");

                // Skin the mesh ourselves from the bones' current pose (exact world positions,
                // independent of how the character is scaled).
                if (!SkinToWorld(smr, _verts)) continue;
                var mesh = smr.sharedMesh;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var tris = mesh.GetTriangles(sub);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        nTris++;
                        Vector3 a = _verts[tris[i]], b = _verts[tris[i + 1]], c = _verts[tris[i + 2]];
                        if (RayTriangle(ray, a, b, c, out float t) && t < hit.distance)
                        {
                            hit.distance = t;
                            hit.point = ray.GetPoint(t);
                            hit.normal = FacingNormal(ray, a, b, c);
                            hit.renderer = smr.name;
                            var w = BoneWeightsOf(smr, tris[i], tris[i + 1], tris[i + 2]);
                            var bone = Dominant(w);
                            hit.bone = Describe(w);
                            hit.part = smr.name.ToLowerInvariant().Contains("glasses") ? BodyPart.Glasses : BreastFrom(g, w);
                            if (hit.part == BodyPart.None) hit.part = ButtFrom(g, w);
                            if (hit.part == BodyPart.None) hit.part = ButtUnderSkirt(g, anim, bone, hit.point);
                            if (hit.part == BodyPart.None) hit.part = Classify(g, anim, bone, hit.point);
                        }
                    }
                }
            }
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf != null ? mf.GetComponent<MeshRenderer>() : null;
                if (mr == null || !mr.enabled || !mf.gameObject.activeInHierarchy || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                nSmr++;
                bool inBounds = mr.bounds.IntersectRay(ray);
                names.Append(mf.name).Append(inBounds ? "+ " : "- ");
                nBounds++;
                var m2 = mf.transform.localToWorldMatrix;
                mf.sharedMesh.GetVertices(_verts);
                for (int sub = 0; sub < mf.sharedMesh.subMeshCount; sub++)
                {
                    var tris = mf.sharedMesh.GetTriangles(sub);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        nTris++;
                        Vector3 ma = m2.MultiplyPoint3x4(_verts[tris[i]]), mb = m2.MultiplyPoint3x4(_verts[tris[i + 1]]), mc = m2.MultiplyPoint3x4(_verts[tris[i + 2]]);
                        if (RayTriangle(ray, ma, mb, mc, out float t) && t < hit.distance)
                        {
                            hit.distance = t;
                            hit.point = ray.GetPoint(t);
                            hit.normal = FacingNormal(ray, ma, mb, mc);
                            hit.renderer = mf.name;
                            hit.bone = mf.transform.parent != null ? mf.transform.parent.name : mf.name;
                            hit.part = Classify(g, anim, mf.transform, hit.point);
                        }
                    }
                }
            }
            LastDiag = $"renderers={nSmr} boundsHit={nBounds} triangles={nTris} [{names}]";
            return hit.distance < float.MaxValue;
        }

        private static readonly List<Vector3> _local = new List<Vector3>();
        private static readonly List<Matrix4x4> _bind = new List<Matrix4x4>();

        /// <summary>Linear blend skinning of the mesh's rest pose with the current bone transforms.</summary>
        private static bool SkinToWorld(SkinnedMeshRenderer smr, List<Vector3> world)
        {
            var mesh = smr.sharedMesh;
            if (!mesh.isReadable) return false;
            var bones = smr.bones;
            mesh.GetVertices(_local);
            mesh.GetBindposes(_bind);
            mesh.GetBoneWeights(_weights);
            world.Clear();
            if (_weights.Count != _local.Count || bones.Length == 0 || _bind.Count != bones.Length)
            {
                var m = smr.transform.localToWorldMatrix;   // not skinned: plain transform
                foreach (var v in _local) world.Add(m.MultiplyPoint3x4(v));
                return true;
            }
            var mats = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                mats[i] = bones[i] != null ? bones[i].localToWorldMatrix * _bind[i] : Matrix4x4.identity;
            for (int i = 0; i < _local.Count; i++)
            {
                var w = _weights[i]; var v = _local[i];
                Vector3 p = mats[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0;
                if (w.weight1 > 0f) p += mats[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1;
                if (w.weight2 > 0f) p += mats[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2;
                if (w.weight3 > 0f) p += mats[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                world.Add(p);
            }
            return true;
        }

        private static Dictionary<Transform, float> BoneWeightsOf(SkinnedMeshRenderer smr, int v0, int v1, int v2)
        {
            var result = new Dictionary<Transform, float>();
            smr.sharedMesh.GetBoneWeights(_weights);
            var bones = smr.bones;
            if (_weights.Count == 0) { if (smr.rootBone != null) result[smr.rootBone] = 1f; return result; }
            var sum = new Dictionary<int, float>();
            foreach (int v in new[] { v0, v1, v2 })
            {
                var w = _weights[v];
                Add(sum, w.boneIndex0, w.weight0); Add(sum, w.boneIndex1, w.weight1);
                Add(sum, w.boneIndex2, w.weight2); Add(sum, w.boneIndex3, w.weight3);
            }
            foreach (var kv in sum)
                if (kv.Key >= 0 && kv.Key < bones.Length && bones[kv.Key] != null)
                    result[bones[kv.Key]] = kv.Value / 3f;
            return result;
        }

        private static Transform Dominant(Dictionary<Transform, float> w)
        {
            Transform best = null; float bw = -1f;
            foreach (var kv in w) if (kv.Value > bw) { bw = kv.Value; best = kv.Key; }
            return best;
        }

        private static string Describe(Dictionary<Transform, float> w)
        {
            var parts = new List<string>();
            foreach (var kv in w) parts.Add($"{kv.Key.name}:{kv.Value:F2}");
            parts.Sort((x, y) => -float.Parse(x.Substring(x.LastIndexOf(':') + 1)).CompareTo(float.Parse(y.Substring(y.LastIndexOf(':') + 1))));
            return string.Join(" ", parts.GetRange(0, Mathf.Min(3, parts.Count)));
        }

        /// <summary>Breast if a breast-physics bone moves this spot noticeably (the spine usually dominates).</summary>
        private static BodyPart BreastFrom(Giantess g, Dictionary<Transform, float> w)
        {
            var outfit = g.variation?.currentOutfitVariation;
            if (outfit == null) return BodyPart.None;
            var lRoots = Roots(outfit.Breast_L); var rRoots = Roots(outfit.Breast_R);
            float l = 0f, r = 0f;
            foreach (var kv in w)
            {
                if (Under(kv.Key, lRoots)) l += kv.Value;
                else if (Under(kv.Key, rRoots)) r += kv.Value;
            }
            if (Mathf.Max(l, r) < BreastShare) return BodyPart.None;
            return l >= r ? BodyPart.BreastLeft : BodyPart.BreastRight;
        }

        private static void Add(Dictionary<int, float> d, int i, float w)
        {
            if (w <= 0f) return;
            d.TryGetValue(i, out float v); d[i] = v + w;
        }

        private static BodyPart Classify(Giantess g, Animator anim, Transform bone, Vector3 point)
        {
            if (bone == null || anim == null || !anim.isHuman) return BodyPart.None;

            // Breasts: the bones driven by the outfit's breast cloth physics.
            var outfit = g.variation?.currentOutfitVariation;
            if (outfit != null)
            {
                if (Under(bone, Roots(outfit.Breast_L))) return BodyPart.BreastLeft;
                if (Under(bone, Roots(outfit.Breast_R))) return BodyPart.BreastRight;
            }
            string n = bone.name.ToLowerInvariant();
            if (n.Contains("breast") || n.Contains("boob") || n.Contains("oppai") || n.Contains("bust"))
                return n.Contains("_r") || n.EndsWith("r") || n.Contains("right") ? BodyPart.BreastRight : BodyPart.BreastLeft;

            var head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null && (bone == head || bone.IsChildOf(head)))
            {
                // Face below the eyes, headpat above them (hair, top of the head).
                // "Up" from the neck to the head bone: the same for every rig (bone axes differ per model).
                var neck = anim.GetBoneTransform(HumanBodyBones.Neck);
                Vector3 up = neck != null && (head.position - neck.position).sqrMagnitude > 1e-8f ? (head.position - neck.position).normalized : head.up;
                float h = Vector3.Dot(point - head.position, up);
                // Eye level: the game's eye objects (or eye bones); the forehead goes up to about
                // the same distance again above the eyes as the mouth is below them.
                Vector3? eyePos = null;
                var eyes = g.GetComponentsInChildren<GiantessEye>();
                if (eyes.Length > 0) { Vector3 sum = Vector3.zero; foreach (var e in eyes) sum += e.transform.position; eyePos = sum / eyes.Length; }
                else { var eb = anim.GetBoneTransform(HumanBodyBones.LeftEye); if (eb != null) eyePos = eb.position; }
                float eyeH = eyePos.HasValue ? Vector3.Dot(eyePos.Value - head.position, up) : 0f;
                float mouthH = g.mouthTransform != null ? Vector3.Dot(g.mouthTransform.position - head.position, up) : eyeH * 0.5f;
                float faceTop = eyeH + Mathf.Max(0f, eyeH - mouthH) * 0.6f;
                bool hair = bone != head;
                // Hair above the eyes and skin above the forehead line: headpat.
                if (hair ? h > eyeH : h > faceTop) return BodyPart.Head;
                // Below the eyes: the face itself, or hair in front of it (closer to the camera than
                // the head bone). Hair hanging behind/at the back is not the face.
                if (bone == head) return FaceDetail(g, point);
                var cam = Camera.main;
                if (cam != null && (point - cam.transform.position).sqrMagnitude < (head.position - cam.transform.position).sqrMagnitude)
                    return FaceDetail(g, point);
                return BodyPart.None;
            }

            foreach (var hb in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
            {
                var t = anim.GetBoneTransform(hb);
                if (t != null && (bone == t || bone.IsChildOf(t))) return BodyPart.Foot;
            }
            foreach (var hb in new[] { HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg })
            {
                var t = anim.GetBoneTransform(hb);
                if (t != null && (bone == t || bone.IsChildOf(t))) return BodyPart.Leg;
            }
            // Helper bones of the rig (e.g. "leg_stretch", "thigh_twist") also belong to the leg.
            string bn = bone.name.ToLowerInvariant();
            if (bn.Contains("leg") || bn.Contains("thigh") || bn.Contains("knee") || bn.Contains("calf") || bn.Contains("shin"))
                return BodyPart.Leg;

            // Tummy: the front of the hips / lower spine (the part facing the camera).
            foreach (var hb in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine })
            {
                var t = anim.GetBoneTransform(hb);
                if (t == null || bone != t) continue;
                var cam = Camera.main;
                if (cam == null || (point - cam.transform.position).sqrMagnitude < (t.position - cam.transform.position).sqrMagnitude)
                    return BodyPart.Tummy;
            }
            return BodyPart.None;
        }

        /// <summary>Butt if the outfit's butt-physics bones move this spot noticeably (Twins).</summary>
        private static BodyPart ButtFrom(Giantess g, Dictionary<Transform, float> w)
        {
            var outfit = g.variation?.currentOutfitVariation;
            if (outfit == null) return BodyPart.None;
            var lRoots = RootsOf(outfit.ButtPhysics_L); var rRoots = RootsOf(outfit.ButtPhysics_R);
            if (lRoots.Count == 0 && rRoots.Count == 0) return BodyPart.None;
            float l = 0f, r = 0f;
            foreach (var kv in w)
            {
                if (Under(kv.Key, lRoots)) l += kv.Value;
                else if (Under(kv.Key, rRoots)) r += kv.Value;
            }
            if (Mathf.Max(l, r) < BreastShare) return BodyPart.None;
            return l >= r ? BodyPart.ButtLeft : BodyPart.ButtRight;
        }

        /// <summary>
        /// The Twins' butt is covered by the coat/skirt (moved by "kilt" bones): a skirt hit behind
        /// the hips, around hip height, counts as the butt (only for outfits with butt physics).
        /// </summary>
        private static BodyPart ButtUnderSkirt(Giantess g, Animator anim, Transform bone, Vector3 point)
        {
            if (bone == null || anim == null || !anim.isHuman) return BodyPart.None;
            var outfit = g.variation?.currentOutfitVariation;
            if (outfit == null || (RootsOf(outfit.ButtPhysics_L).Count == 0 && RootsOf(outfit.ButtPhysics_R).Count == 0)) return BodyPart.None;
            string n = bone.name.ToLowerInvariant();
            if (!(n.Contains("kilt") || n.Contains("skirt") || n.Contains("coat"))) return BodyPart.None;
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            var spine = anim.GetBoneTransform(HumanBodyBones.Spine);
            var lLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var rLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            if (hips == null || lLeg == null || rLeg == null) return BodyPart.None;
            Vector3 root = anim.transform.position;
            Vector3 fwd = anim.transform.forward, up = anim.transform.up;
            // Behind the body?
            if (Vector3.Dot(point - hips.position, fwd) > 0f) return BodyPart.None;
            // Height band: from a bit below the upper-leg joints up to the lower spine.
            float hp = Vector3.Dot(point - root, up);
            float top = Vector3.Dot((spine != null ? spine.position : hips.position) - root, up);
            float hipH = Vector3.Dot(lLeg.position - root, up);
            float span = Mathf.Max(0.01f, top - hipH);
            if (hp < hipH - span * 1.2f || hp > top + span * 0.3f) return BodyPart.None;
            // Her left or right: closer to which upper-leg joint.
            return (point - lLeg.position).sqrMagnitude <= (point - rLeg.position).sqrMagnitude ? BodyPart.ButtLeft : BodyPart.ButtRight;
        }

        internal static List<Transform> RootsOf(MagicaCloth2.MagicaCloth[] cloths)
        {
            var list = new List<Transform>();
            if (cloths != null) foreach (var c in cloths) list.AddRange(Roots(c));
            return list;
        }

        /// <summary>Eye or mouth if the face hit is close to them (sized by the distance between the eyes).</summary>
        private static BodyPart FaceDetail(Giantess g, Vector3 point)
        {
            var eyes = g.GetComponentsInChildren<GiantessEye>();
            float eyeSep = 0f;
            if (eyes.Length >= 2) eyeSep = Vector3.Distance(eyes[0].transform.position, eyes[1].transform.position);
            if (eyeSep <= 0f) return BodyPart.Face;
            // Distance across the view (depth ignored): the eye/mouth points sit inside the head.
            var cam = Camera.main;
            Vector3 view = cam != null ? (point - cam.transform.position).normalized : Vector3.forward;
            float Across(Vector3 a) { Vector3 d = point - a; return (d - Vector3.Dot(d, view) * view).magnitude; }
            foreach (var e in eyes)
                if (Across(e.transform.position) < eyeSep * EyeSize)
                    return HasGlasses(g) ? BodyPart.Glasses : BodyPart.Eye;
            if (g.mouthTransform != null && Across(g.mouthTransform.position) < eyeSep * MouthSize) return BodyPart.Mouth;
            return BodyPart.Face;
        }

        /// <summary>Visible glasses on the character (Blue).</summary>
        internal static bool HasGlasses(Giantess g)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>())
                if (r != null && r.enabled && r.gameObject.activeInHierarchy && r.name.ToLowerInvariant().Contains("glasses")) return true;
            return false;
        }

        internal static float EyeSize = 0.3f, MouthSize = 0.45f;

        private static List<Transform> Roots(MagicaCloth2.MagicaCloth c)
        {
            var list = new List<Transform>();
            var bones = c != null ? c.SerializeData?.rootBones : null;
            if (bones != null) foreach (var b in bones) if (b != null) list.Add(b);
            return list;
        }

        private static bool Under(Transform bone, List<Transform> roots)
        {
            foreach (var r in roots) if (bone == r || bone.IsChildOf(r)) return true;
            return false;
        }

        // Möller–Trumbore, two-sided.
        private static bool RayTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            t = 0f;
            Vector3 e1 = b - a, e2 = c - a, p = Vector3.Cross(ray.direction, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-9f) return false;
            float inv = 1f / det;
            Vector3 s = ray.origin - a;
            float u = Vector3.Dot(s, p) * inv; if (u < 0f || u > 1f) return false;
            Vector3 q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(ray.direction, q) * inv; if (v < 0f || u + v > 1f) return false;
            t = Vector3.Dot(e2, q) * inv;
            return t > 0f;
        }
    }
}
