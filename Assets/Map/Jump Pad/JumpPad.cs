using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Map
{
    [RequireComponent(typeof(Animator))]
    public class JumpPad : EntityComponent
    {
        private static readonly int Pressed = Animator.StringToHash("Pressed");

        //fields////////////////////////////////////////////////////////////////////////////////////////////////////////
        [SerializeField] private float impulse;
        [SerializeField] private float timeOffset;
        [SerializeField] private float cooldown;
        [SerializeField] private Animator animator;

        [SerializeField] private UnityEvent onJump;

        private readonly List<(Collider2D col, bool isPlayer, float time)> _bodiesInside = new();

        //initialisation////////////////////////////////////////////////////////////////////////////////////////////////
        protected override void Wake()
        {
            Assert.IsNotNull(animator);
        }
        public override void Initialise() {}
        public override void Activate() { }

        //public interface//////////////////////////////////////////////////////////////////////////////////////////////
        public override string JsonName => "jumpPad";
        public override IEnumerator<PropertyHandle> GetProperties()
        {
            yield return new PropertyHandle()
            {
                PropertyName = "Impulse",
                PropertyType = PropertyType.Decimal,
                Getter = () => impulse,
                Setter = (object input) => impulse = (float)input
            };
        }

        public override JSONNode ExtractData()
        {
            var json = new JSONObject();
            json["impulse"] = impulse;
            return json;
        }

        public override void Replicate(JSONNode data)
        {
            impulse = data["impulse"].AsFloat;
        }

        
        //game events///////////////////////////////////////////////////////////////////////////////////////////////////
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") && !other.CompareTag("Corpse"))
                return;

            var body = (other, other.CompareTag("Player"), Time.time);
            _bodiesInside.Add(body);

            StartCoroutine(Push(body));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            _bodiesInside.RemoveAll((x) => x.col == other);
        }

        private void Update()
        {
            for (var i = 0; i < _bodiesInside.Count; i++)
                if (_bodiesInside[i].time - Time.time >= cooldown)
                {
                    _bodiesInside[i] = (_bodiesInside[i].col, _bodiesInside[i].isPlayer, Time.time);
                    StartCoroutine(Push(_bodiesInside[i]));
                }
        }

        
        //private logic/////////////////////////////////////////////////////////////////////////////////////////////////
        private IEnumerator Push((Collider2D col, bool isPlayer, float time) pressingBody)
        {
            onJump.Invoke();
            animator.SetBool(Pressed, true);
            yield return new WaitForSeconds(timeOffset);
            animator.SetBool(Pressed, false);
        }
    }
}