using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public struct SkillInfo
    {
        public int cost;
        public float value;
        public string spritePath;
    }

    public class PlayerPawn : MapPawn
    {
        public float _health;
        public float _maxHealth;

        public float _attack;
        public float _attackTimes = 1.0f;
        public float _attackPlusConst = 0.0f;

        public float _defence;
        public int _experience;
        public int _upgradeExperience;
        public int _level;

        public List<SkillInfo> skillInfos;

        public PlayerPawn(BlockInfo block, GameObject gameObject, TeamType teamType, int moveRange, int attackRange, float health, float attack, float defence, int upgradeExperience) :
            base(block, gameObject, teamType, moveRange, attackRange)
        {
            _health = health;
            _maxHealth = health;
            _attack = attack;
            _defence = defence;
            _experience = 0;
            _upgradeExperience = upgradeExperience;
            _level = 1;
            /*
            skillInfos = new List<SkillInfo>();
            skillInfos.Add(new SkillInfo()
            {
                cost = 1,
                value = attack,
                spritePath = "attack"
            });
            skillInfos.Add(new SkillInfo()
            {
                cost = 1,
                value = defence,
                spritePath = "defence"
            });
            */
            EventMgr.Instance.Register<MapPawn, MapPawn>(EventType.EVENT_BATTLE_UI, SubEventType.PLAYER_ATTACK_FINISH, RecoverAttack);
        }

        public bool TryUpGrade()
        {
            if(_experience < _upgradeExperience)
            {
                return false;
            }
            _experience -= _upgradeExperience;
            return true;
        }

        public override float TakeDamage(MapPawn damageSource, float damageValue)
        {
            _defence -= damageValue;
            if (_defence < 0)
            {
                _health += _defence;
                _defence = 0;
            }
            UpdateGo();
            return damageValue;
        }

        public override float GetAttackValue()
        {
            return (_attack + _attackPlusConst) * _attackTimes;
        }

        protected override void UpdateGo()
        {
            base.UpdateGo();
            _pawnGo.UpdateStates(_health, (_attack + _attackPlusConst) * _attackTimes, _defence);
        }

        public void UpadateAttackTimes(float value, bool bIsConst, bool bIsPlus)
        {
            if(bIsConst)
            {
                _attackPlusConst += value;
            }
            else
            {
                if(!bIsPlus)
                {
                    _attackTimes *= value;
                }
            }
            UpdateGo();
        }

        public void RecoverAttack(MapPawn pawnA, MapPawn pawnB)
        {
            _attackTimes = 1.0f;
            UpdateGo();
        }

        public void AddDefence(float value)
        {
            _defence += value;
            UpdateGo();
        }
    }
}
