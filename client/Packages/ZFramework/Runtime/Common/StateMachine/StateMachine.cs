using System;
using System.Collections.Generic;

namespace ZLib
{
    /// <summary>
    /// 状态机(密封类)
    /// </summary>
    sealed public class StateMachine<CONTEXT>
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        public IState<CONTEXT> currentState { get; private set; }

        /// <summary>
        /// 状态改变的时候触发.
        /// </summary>
        public Action<StateMachine<CONTEXT>> stateChange;

        private Queue<IState<CONTEXT>> commandQueue = new Queue<IState<CONTEXT>>();

        //状态字典
        private Dictionary<Type, IState<CONTEXT>> _states = new Dictionary<Type, IState<CONTEXT>>();

        /// <summary>
        /// 状态机改变类型
        /// </summary>
        public StateChangeType changeType = StateChangeType.Default;

        /// <summary>
        /// 上下文状态
        /// </summary>
        public CONTEXT context
        {
            get;
            private set;
        }

        public StateMachine(CONTEXT context)
        {
            this.context = context;
        }

        public void Update()
        {
            switch (changeType)
            {
                case StateChangeType.Default:

                    while (commandQueue.Count > 0)
                    {
                        var state = commandQueue.Dequeue();

                        //校验同一性
                        if (currentState != null)

                            if (currentState == state)
                                continue;

                        ChangeTo(state);
                    }
                    break;
                case StateChangeType.Immediately:
                    break;
                default:
                    break;
            }


            if (currentState != null)
            {
                currentState.OnUpdate();
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Destroy()
        {
            var ie = _states.GetEnumerator();

            while (ie.MoveNext())
            {
                ie.Current.Value.OnDestroy();
            }
            //foreach (var state in _states)
            //{
            //    state.Value.OnDestroy();
            //}

            //清空字典
            _states.Clear();
        }

        /// <summary>
        /// 添加状态
        /// </summary>
        /// <param name="__state"></param>
        public void AddState(IState<CONTEXT> __state)
        {
            if (_states.ContainsKey(__state.GetType()))
            {
                throw new Exception("状态已经存在");
            }
            __state.context = context;
            _states.Add(__state.GetType(), __state);
        }

        /// <summary>
        /// 改变状态
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ChangeState<STATE>() where STATE : IState<CONTEXT>
        {
            IState<CONTEXT> state = null;

            if (_states.TryGetValue(typeof(STATE), out state))
            {
                switch (changeType)
                {
                    case StateChangeType.Default:

                        commandQueue.Enqueue(state);
                        break;
                    case StateChangeType.Immediately:

                        ChangeTo(state);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                throw new Exception("Can't Find State " + typeof(STATE));
            }
        }

        /// <summary>
        /// 改变状态
        /// </summary>
        /// <param name="_type"></param>
        public void ChangeState(Type _type)
        {
            if (_states.TryGetValue(_type, out IState<CONTEXT> state))
            {
                switch (changeType)
                {
                    case StateChangeType.Default:

                        commandQueue.Enqueue(state);
                        break;
                    case StateChangeType.Immediately:

                        ChangeTo(state);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                throw new Exception("Can't Find State " + _type);
            }
        }

        /// <summary>
        /// 改变到一个状态
        /// </summary>
        /// <param name="state"></param>
        private void ChangeTo(IState<CONTEXT> state)
        {
            if (currentState != null)
                ///执行退出
                currentState.OnExit();

            //执行进入
            currentState = state;

            if (currentState != null)
                //执行进入
                currentState.OnEnter();

            if (stateChange != null)
                stateChange(this);
        }

        /// <summary>
        /// 预先可以拿到某一个状态
        /// </summary>
        /// <typeparam name="STATE"></typeparam>
        /// <returns></returns>
        public STATE PeekState<STATE>() where STATE : class, IState<CONTEXT>
        {
            if (_states.TryGetValue(typeof(STATE), out IState<CONTEXT> state))
            {
                return state as STATE;
            }
            else
            {
                throw new Exception("Can't Find State:" + typeof(STATE));
            }
        }

        /// <summary>
        /// 预先可以拿到某一个状态
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public IState<CONTEXT> PeekState(Type _type)
        {
            if (_states.TryGetValue(_type, out IState<CONTEXT> state))
            {
                return state as IState<CONTEXT>;
            }
            else
            {
                throw new Exception("Can't Find State:" + _type);
            }
        }

        /// <summary>
        /// 执行
        /// </summary>
        public void Execute()
        {
            if (currentState != null)
            {
                currentState.OnExecute();
            }
        }
    }

    public enum StateChangeType
    {
        /// <summary>
        /// 延迟一帧
        /// </summary>
        Default,

        /// <summary>
        /// 立即切换
        /// </summary>
        Immediately,
    }
}
