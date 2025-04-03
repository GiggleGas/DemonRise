using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace PDR
{
    public class MapPawn
    {
        public GameObject _gameObject;
        public Animator _animator;
        public Vector2Int _gridPosition;

        public MapPawn(Vector2Int gridPosition, GameObject gameObject)
        {
            _gridPosition = gridPosition;
            _gameObject = gameObject;
            _animator = _gameObject.GetComponent<Animator>();
        }

        public void UpdatePawnLocation(Vector2 Location, Vector2Int gridPosition)
        {
            _gridPosition = gridPosition;
            _gameObject.GetComponent<Transform>().position = Location;
        }

        public Transform GetTransform() { return _gameObject.transform; }
        public void UpdateGo(GameObject gameObject)
        {
            _gameObject = gameObject;
            _animator = _gameObject.GetComponent<Animator>();
        }
        public void PlayAnimation(string animation, float crossFade = 0.2f)
        {
            _animator.CrossFade(animation, crossFade);
        }
    }
}
