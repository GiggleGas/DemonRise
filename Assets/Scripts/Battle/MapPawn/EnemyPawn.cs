using PDR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class EnemyPawn : MapPawn
    {
        public float _health;
        public float _maxHealth;
        public float _attack;
        public float _defence;
        public int _experience;
        public int _energy;

        public EnemyPawn(BlockInfo block, GameObject gameObject, TeamType teamType, int moveRange, int attackRange, float health, float maxHealth, float attack, int exp) : 
            base(block, gameObject, teamType, moveRange, attackRange)
        {
            _health = health;
            _maxHealth = maxHealth;
            _attack = attack;
            _experience = exp;
        }

        public override float TakeDamage(MapPawn damageSource, float damageValue)
        {
            _health -= damageValue;
            UpdateGo();
            return damageValue;
        }

        public override float GetAttackValue()
        {
            return _attack;
        }

        protected override void UpdateGo()
        {
            base.UpdateGo();
            _pawnDisplayComp.UpdateStates(_health, _attack, _defence);
        }

        public bool CanAttack()
        {
            return false;
        }

        public void Enhance(float health, float attack, float defence)
        {
            _health += health;
            _attack += attack;
            _defence += defence;
            UpdateGo();
        }

        /// <summary>
        /// 当开始初始化该Monster
        /// </summary>
        public void BeginUpdate()
        {
            _energy = 3; // 假设初始化为3点能量
        }
    }
}
