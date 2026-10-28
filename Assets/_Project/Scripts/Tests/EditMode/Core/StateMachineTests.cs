using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NoWayOut.Core.States;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NoWayOut.Tests.EditMode.Core
{
    public sealed class StateMachineTests
    {
        private List<string> log;
        private TestStateMachine stateMachine;


        [SetUp]
        public void SetUp()
        {
            log = new List<string>();
            FakeStateFactory factory = new FakeStateFactory();
            factory.Add(new FirstState(log));
            factory.Add(new SecondState(log));
            factory.Add(new PayloadState(log));
            stateMachine = new TestStateMachine(factory);
        }


        [UnityTest]
        public IEnumerator EnterAsync_NoActiveState_EntersRequestedState()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await stateMachine.EnterAsync<FirstState>();

                CollectionAssert.AreEqual(new[] { "First.Enter" }, log);
                Assert.IsInstanceOf<FirstState>(stateMachine.ActiveState);
            });
        }


        [UnityTest]
        public IEnumerator EnterAsync_WithActiveState_ExitsActiveStateFirst()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await stateMachine.EnterAsync<FirstState>();
                await stateMachine.EnterAsync<SecondState>();

                CollectionAssert.AreEqual(new[] { "First.Enter", "First.Exit", "Second.Enter" }, log);
            });
        }


        [UnityTest]
        public IEnumerator EnterAsync_PayloadedState_ReceivesPayload()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await stateMachine.EnterAsync<PayloadState, int>(42);

                CollectionAssert.AreEqual(new[] { "Payload.Enter 42" }, log);
            });
        }


        private sealed class TestStateMachine : StateMachine
        {
            public TestStateMachine(IStateFactory stateFactory) : base(stateFactory)
            {
            }
        }


        private sealed class FakeStateFactory : IStateFactory
        {
            private readonly List<IExitableState> states = new List<IExitableState>();


            public void Add(IExitableState state)
            {
                states.Add(state);
            }


            public TState Create<TState>() where TState : class, IExitableState
            {
                foreach (IExitableState state in states)
                {
                    if (state is TState typedState)
                    {
                        return typedState;
                    }
                }

                return null;
            }
        }


        private sealed class FirstState : IState
        {
            private readonly List<string> log;


            public FirstState(List<string> log)
            {
                this.log = log;
            }


            public UniTask EnterAsync()
            {
                log.Add("First.Enter");
                return UniTask.CompletedTask;
            }


            public UniTask ExitAsync()
            {
                log.Add("First.Exit");
                return UniTask.CompletedTask;
            }
        }


        private sealed class SecondState : IState
        {
            private readonly List<string> log;


            public SecondState(List<string> log)
            {
                this.log = log;
            }


            public UniTask EnterAsync()
            {
                log.Add("Second.Enter");
                return UniTask.CompletedTask;
            }


            public UniTask ExitAsync()
            {
                log.Add("Second.Exit");
                return UniTask.CompletedTask;
            }
        }


        private sealed class PayloadState : IPayloadedState<int>
        {
            private readonly List<string> log;


            public PayloadState(List<string> log)
            {
                this.log = log;
            }


            public UniTask EnterAsync(int payload)
            {
                log.Add("Payload.Enter " + payload);
                return UniTask.CompletedTask;
            }


            public UniTask ExitAsync()
            {
                log.Add("Payload.Exit");
                return UniTask.CompletedTask;
            }
        }
    }
}
