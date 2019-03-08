using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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

    struct SegInfo
    {
        public Vector3 fromPos;
        public Vector3 toPos;
        public float length;

        public override string ToString()
        {
            return string.Format("fromPos={0},toPos={1},length={2}",fromPos,toPos,length);
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
        public float duration = 0;
        public FacingType facingType = FacingType.KeepOriginal;
        public LoopType loopType = LoopType.Restart;              // TODO
        public int loops = 1;
        public event Action<int> onSegEnd;
        public event Action<int> onLoopEnd;
        [HideInInspector]
        public Vector3[] pathPoints;


        bool played = false;
        float length;
        readonly List<SegInfo> segInfoList = new List<SegInfo>();

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
                    fromPos = pathPoints[i],
                    toPos = pathPoints[i+1],
                    length = curLength,
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

        private void OnDrawGizmos()
        {
            // TODO
        }

        void Update()
        {
            if(valid)
            {
                rectTransform.anchoredPosition3D = Vector3.Lerp(curSegInfo.fromPos, curSegInfo.toPos, curSegProgess / curSegInfo.length);
                curSegProgess += speed * Time.deltaTime;
                while (curSegProgess > curSegInfo.length)
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
                scale.x = scaleX * (curSegInfo.toPos.x - curSegInfo.fromPos.x > 0 ? 1 : -1);
                transform.localScale = scale;
                break;
            case FacingType.ForwardAlways:
                var angle = Vector2.SignedAngle(Vector2.right, curSegInfo.toPos - curSegInfo.fromPos);
                transform.localEulerAngles = Vector3.forward * angle;
                break;
            case FacingType.KeepOriginal:
            default:
                break;
            }
        }

    }

}
