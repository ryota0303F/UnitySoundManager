using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSound
{
    public class SoundList : MonoBehaviour
    {
        [SerializeField] AudioClip[] audioClipList;

        [SerializeField] string[] tagClipList;

        Dictionary<string, AudioClip> _audioClipList = null;


        void Start()
        {
            SetDictionary();
        }

        void SetDictionary()
        {
            _audioClipList = new Dictionary<string, AudioClip>();
            for (int i = 0; i < audioClipList.Length; i++)
            {
                if (tagClipList.Length <= i)
                    break;

                _audioClipList.Add(tagClipList[i], audioClipList[i]);
            }
        }

        public Dictionary<string, AudioClip> GetDictionary()
        {
            return _audioClipList;
        }

    }
}