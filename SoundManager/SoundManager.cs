//------------------------------------------------------------------
//  名称　SoundManagerクラス　
//  機能　BGM、SEすべてを管理する
//　更新日　2021 / 10 / 10
//------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSound
{
    //使用するサウンドの種類
    public enum UsingSoundTyp
    {
        None,
        SE,         //効果音
        BGM,        //BGM
        SE_3D,      //3D(今後実装予定)
    }

    //サウンドの再生方法
    public enum PlaySoundTyp
    {
        None,
        Single,     //一回再生する
        Loop,       //一定条件が来るまでループ
        LoopNum,    //指定回数ループ
    }

    /// <summary>
    /// サウンドの通知
    /// </summary>
    public class SoundInfo
    {
        public SoundInfo() { }

        //サウンドのタイプ
        public UsingSoundTyp m_usingSoundTyp;   //念のためつけてるけど現在の使用用途は謎

        //サウンドの再生方法
        public PlaySoundTyp m_playSoundTyp;

        //使用するサウンドの文字列
        public string m_usingSound;

        //使用するサウンドがLoopだった際に何回繰り返すか
        public int m_loopNum = -1;

        //サウンド終了時に呼ばれる関数の登録
        public Func<bool> m_endFunc;

        //サウンドを一時停止させるかの確認用関数の登録
        //trueでストップ
        public Func<bool> m_pauseFunc;

        //一時停止させたサウンドをPlayするかの確認用関数の登録
        //trueでPlay
        public Func<bool> m_unpauseFunc;

        //SoundPlayerを終了させる
        //trueで終了
        public Func<bool> m_stopFunc;
    }
    
    /// <summary>
    /// サウンドを再生するためのクラス
    /// </summary>
    public class SoundPlayer
    {
        public SoundPlayer(SoundInfo _soundInfo, AudioSource _audioSource, int _index)
        {
            m_soundInfo = _soundInfo;
            m_audioSource = _audioSource;
            m_index = _index;
            Inst();
        }

        //通知データ
        public SoundInfo m_soundInfo = null;

        //使用しているAudioSource
        public AudioSource m_audioSource = null;

        //使用中のindex
        public int m_index = -1;

        int m_loopCount;

        bool m_playFlg = true;

        /// <summary>
        /// 初期化
        /// </summary>
        void Inst()
        {
            m_loopCount = 0;
            m_playFlg = true;
            m_audioSource.PlayDelayed(0);
        }

        /// <summary>
        /// SoundPlayerの終了
        /// </summary>
        void ReleaseAudioSource()
        {
            if (m_soundInfo.m_endFunc != null) 
                m_soundInfo.m_endFunc();

            SoundManager.Instance.ReleaseAudioSource(m_index, this);
        }

        /// <summary>
        /// サウンド終了後の処理
        /// </summary>
        void SoundEnd()
        {
            switch(m_soundInfo.m_playSoundTyp)
            {
                case PlaySoundTyp.Single:       //1回のみ
                    ReleaseAudioSource();
                    break;
                case PlaySoundTyp.Loop:         //ループ
                    m_audioSource.PlayDelayed(0);
                    break;
                case PlaySoundTyp.LoopNum:      //指定回数ループ
                    m_loopCount++;
                    if (m_loopCount >= m_soundInfo.m_loopNum)
                    {
                        ReleaseAudioSource();
                    }
                    else
                    {
                        m_audioSource.PlayDelayed(0);
                    }
                    break;
                default:
                    ReleaseAudioSource();
                    break;
            }

        }

        /// <summary>
        /// 更新
        /// </summary>
        public void Update()
        {
            //一時停止
            if(m_soundInfo.m_pauseFunc != null&& m_playFlg)
            {
                if(m_soundInfo.m_pauseFunc())
                {
                    m_audioSource.Pause();
                    m_playFlg = false;
                }
            }

            //再開
            if (m_soundInfo.m_unpauseFunc != null && !m_playFlg)
            {
                if (m_soundInfo.m_unpauseFunc())
                {
                    m_audioSource.UnPause();
                    m_playFlg = true;
                }
            }

            //SoundPlayerの終了
            if (m_soundInfo.m_stopFunc != null)
            {
                if (m_soundInfo.m_stopFunc())
                {
                    m_audioSource.Stop();
                    m_playFlg = false;
                    ReleaseAudioSource();
                }
            }

            //サウンド終了時
            if (!m_audioSource.isPlaying && m_playFlg)
            {
                SoundEnd();
            }
        }


    }

    public class SoundManager : Singleton<SoundManager>
    {
        [Header("SoundManagerは常にHierarchy下になるようにする")]

        [SerializeField] SoundList _soundList = null;

        //オブジェクトプール
        [SerializeField] AudioSource[] _audioSourceList = null;
        List<bool> _audioSourceListFlg = null;

        //サウンドリスト
        Dictionary<string, AudioClip> _audioClipList = null;

        //通知リスト
        public List<SoundInfo> _soundInfosList { get; private set; } = null;

        //再生しているサウンドのリスト
        public List<SoundPlayer> _soundPlayerList { get; private set; } = null;

        //SoundPlayerリストの解放予定のリスト
        List<SoundPlayer> _releasesoundPlayerList = null;

        void Start()
        {
            Inst();
        }
        
        void Update()
        {
            InfoListCheck();
            SoundUpdate();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Inst()
        {
            //AudioClipDictionaryの取得
            _soundInfosList = new List<SoundInfo>();
            if (_soundList != null)
            {
                _audioClipList = _soundList.GetDictionary();
            }
            else
            {
                Debug.LogError("Could not get 'SoundList'");
                return;
            }

            //AudioSourceListの準備
            if (_audioSourceList != null)
            {
                _audioSourceListFlg = new List<bool>();
                for (int i = 0; i < _audioSourceList.Length; i++)
                {
                    _audioSourceListFlg.Add(false);
                }
            }
            else
            {
                Debug.LogError("There is no 'AudioSourceList'");
                return;
            }

            if(_soundPlayerList == null)
                _soundPlayerList = new List<SoundPlayer>();

            if (_releasesoundPlayerList == null) 
                _releasesoundPlayerList = new List<SoundPlayer>();

        }

        /// <summary>
        /// 通知チェック
        /// </summary>
        void InfoListCheck()
        {
            if (_soundInfosList.Count <= 0)
                return;

            foreach(var info in _soundInfosList)
            {
                if(!_audioSourceListFlg.Contains(false))
                {
                    Debug.LogError("No 'AudioSource' available");
                    continue;
                }

                if (!_audioClipList.ContainsKey(info.m_usingSound))
                {
                    Debug.LogError($"No '{info.m_usingSound}' AudioClip");
                    continue;
                }

                AudioClip audioClip = _audioClipList[info.m_usingSound];

                int index = 0;
                for (int i = 0; i < _audioSourceListFlg.Count; i++)
                {
                    if (!_audioSourceListFlg[i])
                    {
                        index = i;
                        _audioSourceListFlg[i] = true;
                        break;
                    }
                }

                _audioSourceList[index].clip = audioClip;

                SoundPlayer soundPlayer = new SoundPlayer(info, _audioSourceList[index], index);
                _soundPlayerList.Add(soundPlayer);
            }
            _soundInfosList.Clear();
        }

        /// <summary>
        /// 通知を取得
        /// </summary>
        /// <param name="_soundInfo"></param>
        public void SetInfo(SoundInfo _soundInfo)
        {
            _soundInfosList.Add(_soundInfo);
        }

        /// <summary>
        /// 使用中のAudioSourceの解放
        /// </summary>
        /// <param name="_index"></param>
        public void ReleaseAudioSource(int _index, SoundPlayer _soundPlayer)
        {
            _audioSourceList[_index].clip = null;
            _audioSourceListFlg[_index] = false;

            _releasesoundPlayerList.Add(_soundPlayer);
        }

        /// <summary>
        /// 解放予定リストの確認
        /// </summary>
        void ReleaseCheck()
        {
            //解放予定があれば解放
            if (_releasesoundPlayerList.Count > 0)
            {
                foreach (var _soundPlayer in _releasesoundPlayerList)
                {
                    _soundPlayerList.Remove(_soundPlayer);
                }
                _releasesoundPlayerList.Clear();
            }
        }

        /// <summary>
        /// サウンドの更新
        /// </summary>
        void SoundUpdate()
        {
            if (_soundPlayerList == null)
                return;

            foreach(var _soundPlayer in _soundPlayerList)
            {
                _soundPlayer.Update();
            }
            ReleaseCheck();
        }


    }
}