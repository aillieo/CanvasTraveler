using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;

namespace AillieoUtils.UI
{

    public enum FacingType
    {
        KeepOriginal,
        LeftOrRight,
        ForwardAlways,
    }

    public enum LoopType
    {
        PingPong,
        Restart,
    }

    [Serializable]
    public class LoopEndEvent : UnityEvent<int> { }
    [Serializable]
    public class SegEndEvent : UnityEvent<int> { }


    class SegInfo
    {
        Vector3 mFromPos;
        public Vector3 FromPos
        {
            get{ return !isReversed ? mFromPos : mToPos; }
            set{ mFromPos = value; }
        }
        public Vector3 mToPos;
        public Vector3 ToPos
        {
            get{ return !isReversed ? mToPos : mFromPos; }
            set{ mToPos = value; }
        }
        public float length;
        public bool isReversed;

        public override string ToString()
        {
            return string.Format("fromPos={0},toPos={1},length={2}",mFromPos,mToPos,length);
        }
    }

    class SegInfoList
    {
        readonly List<SegInfo> segInfoList = new List<SegInfo>();

        public bool IsReversed { get; private set; }
        public int Count { get { return segInfoList.Count; } }
        public void Add(SegInfo segInfo) { segInfoList.Add(segInfo);}
        public void Clear() {
            segInfoList.Clear();
            IsReversed = false;
        }

        public void Reverse()
        {
            IsReversed = !IsReversed;
            foreach(var si in segInfoList)
            {
                si.isReversed = IsReversed;
            }
        }

        public SegInfo this[int index] {
            get { return !IsReversed ? segInfoList[index] : segInfoList[segInfoList.Count - 1 - index]; }
            set {
                if (!IsReversed)
                {
                    segInfoList[index] = value;
                }
                else
                {
                    segInfoList[segInfoList.Count - 1 - index] = value;
                }
            }
        }

    }


    [RequireComponent(typeof(RectTransform))]
    public class CanvasTraveler : MonoBehaviour
    {
        RectTransform mRectTransform;
        RectTransform rectTransform
        {
            get
            {
                if(null == mRectTransform)
                {
                    mRectTransform = GetComponent<RectTransform>();
                }
                return mRectTransform;
            }
        }

        public bool autoPlay = true;
        public bool restartOnEnable = true;
        public float duration = 1;
        public FacingType facingType = FacingType.KeepOriginal;
        public LoopType loopType = LoopType.Restart;
        public int loops = 3;
        public bool IsReversed { get; private set; } = false;

        [Space]

        public SegEndEvent onSegEnd;
        public LoopEndEvent onLoopEnd;
        [HideInInspector]
        public Vector3[] pathPoints = Array.Empty<Vector3>();


        bool played = false;
        float length;
        readonly SegInfoList segInfoList = new SegInfoList();

        SegInfo curSegInfo;
        int curSegIndex;
        int curLoopIndex;
        float curSegProgess = 0;
        float speed = 0;

        bool valid;

        void Reset()
        {
            duration = Mathf.Clamp(duration, float.Epsilon, duration);

            CleanUp();
            float curLength = 0;
            for(int i = 0; i < pathPoints.Length - 1; ++ i)
            {
                curLength = (pathPoints[i+1] - pathPoints[i]).magnitude;
                if(curLength == 0)
                    continue;

                SegInfo si = new SegInfo
                {
                    FromPos = pathPoints[i],
                    ToPos = pathPoints[i + 1],
                    length = curLength,
                    isReversed = false,
                };
                segInfoList.Add(si);
                length += curLength;
            }

            valid = segInfoList.Count > 0;

            if (valid)
            {
                speed = length / duration;
            }

        }


        void CleanUp()
        {
            segInfoList.Clear();
            length = 0;
            speed = 0;
            curSegIndex = 0;
            curLoopIndex = 0;
            IsReversed = false;
            valid = false;
        }


        public void Restart()
        {
            Reset();
            if (valid)
            {
                curSegInfo = segInfoList[0];
                UpdateRotation();
            }
        }

        void OnEnable()
        {
            // first time
            if(autoPlay && !played)
            {
                played = true;
                Restart();
            }
            // else
            else if(restartOnEnable)
            {
                Restart();
            }
        }


        void OnDisable()
        {
            
        }

        Vector3 CanvasPosToWorldPos(Vector3 anchoredPosition3D)
        {
            if(null == transform.parent)
            {
                return Vector3.zero;
            }
            return transform.parent.TransformPoint(anchoredPosition3D);
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            for(int i = 0; i < segInfoList.Count; ++i)
            {
                Gizmos.DrawLine(CanvasPosToWorldPos(segInfoList[i].FromPos), CanvasPosToWorldPos(segInfoList[i].ToPos));
            }
        }

        void Update()
        {
            if(valid)
            {
                rectTransform.anchoredPosition3D = Vector3.Lerp(curSegInfo.FromPos, curSegInfo.ToPos, curSegProgess / curSegInfo.length);
                curSegProgess += speed * Time.deltaTime;
                while (valid && curSegProgess > curSegInfo.length)
                {
                    OnSegEnd();
                }
            }
        }

        void OnLoopEnd()
        {
            onLoopEnd?.Invoke(curLoopIndex);
            curLoopIndex++;
            if(loops < 0 || curLoopIndex < loops)
            {
                if(loopType == LoopType.PingPong)
                {
                    segInfoList.Reverse();
                    IsReversed = segInfoList.IsReversed;
                }
                curSegIndex = 0;
                curSegInfo = segInfoList[curSegIndex];
                UpdateRotation();
                curSegProgess = 0;
            }
            else
            {
                valid = false;
            }
        }

        void OnSegEnd()
        {
            onSegEnd?.Invoke(curSegIndex);
            curSegProgess -= curSegInfo.length;
            curSegIndex++;
            if (curSegIndex < segInfoList.Count)
            {
                curSegInfo = segInfoList[curSegIndex];
                UpdateRotation();
            }
            else
            {
                OnLoopEnd();
            }
        }

        void UpdateRotation()
        {
            switch (facingType)
            {
            case FacingType.LeftOrRight:
                var scale = transform.localScale;
                var scaleX = Mathf.Abs(scale.x);
                scale.x = scaleX * (curSegInfo.ToPos.x - curSegInfo.FromPos.x > 0 ? 1 : -1);
                transform.localScale = scale;
                break;
            case FacingType.ForwardAlways:
                var angle = Vector2.SignedAngle(Vector2.right, curSegInfo.ToPos - curSegInfo.FromPos);
                transform.localEulerAngles = Vector3.forward * angle;
                break;
            case FacingType.KeepOriginal:
            default:
                break;
            }
        }

    }

}
