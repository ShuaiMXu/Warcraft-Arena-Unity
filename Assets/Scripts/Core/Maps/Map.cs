﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Core
{
    public class Map
    {
        private readonly WorldGrid mapGrid = new WorldGrid();
        private readonly List<WorldEntity> worldEntities = new List<WorldEntity>();

        public MapDefinition Definition { get; private set; }
        public MapSettings Settings { get; private set; }

        protected float VisibleDistance { get; set; }
        protected int UnloadTimer { get; set; }

        public Map(int id, Map parent = null)
        {
            Definition = BalanceManager.MapsById.LookupEntry(id);
        }

        public void Initialize(Scene mapScene)
        {
            foreach (var rootObject in mapScene.GetRootGameObjects())
            {
                Settings = rootObject.GetComponentInChildren<MapSettings>();

                if (Settings != null)
                    break;
            }

            Assert.IsNotNull(Settings, $"Map settings are missing in map: {Definition.MapName} Id: {Definition.Id}");
            Assert.IsTrue(Settings.GridCells.Count % Settings.GridLayout.constraintCount == 0, $"Grid in map: {Definition.MapName} Id: {Definition.Id} should be filled rect!");
            mapGrid.Initialize(this, GridHelper.MinUnloadDelay, Definition.MapType != MapType.Common);
        }

        public void Deinitialize()
        {
            mapGrid.Deinitialize();
            Settings = null;
        }

        public virtual void DoUpdate(int deltaTime) { }
        public virtual void DelayedUpdate(int deltaTime) { }

        public void SearchAreaTargets(List<Unit> targets, float radius, Vector3 center, Unit referer, TargetChecks checkType)
        {
            Collider[] hitColliders = Physics.OverlapSphere(center, radius, 1 << LayerMask.NameToLayer("Characters"));
            for (int i = 0; i < hitColliders.Length; i++)
            {
                var targetUnit = hitColliders[i].gameObject.GetComponent<Unit>();
                if (targetUnit == null || targetUnit.Map != this)
                    continue;

                switch (checkType)
                {
                    case TargetChecks.Ally:
                        if (referer.IsHostileTo(targetUnit))
                            continue;
                        break;
                    case TargetChecks.Enemy:
                        if (!referer.IsHostileTo(targetUnit))
                            continue;
                        break;
                }

                targets.Add(targetUnit);
            }
        }

        public void Visit(GridCell cell, IEntityGridVisitor gridVisitor) { throw new NotImplementedException(); }
        public bool UnloadGrid(WorldGrid worldGrid, bool pForce) { throw new NotImplementedException(); }
        public virtual void UnloadAll() {  }

        public bool IsBattlegroundOrArena() { throw new NotImplementedException(); }
  

        public void AddWorldObject(WorldEntity obj)
        {
            worldEntities.Add(obj);
        }

        public void RemoveWorldObject(WorldEntity obj)
        {
            worldEntities.Remove(obj);
        }

        public void VisitAll(float x, float y, float radius, ref IEntityGridVisitor notifier) { throw new NotImplementedException(); }
        public void VisitWorld(float x, float y, float radius, ref IWorldGridVisitor notifier) { throw new NotImplementedException(); }
        public void VisitGrid(float x, float y, float radius, ref IGridGridVisitor notifier) { throw new NotImplementedException(); }

        public TEntity FindMapEntity<TEntity>(ulong networkId) where TEntity : Entity
        {
            return worldEntities.Find(entity => entity.NetworkId == networkId) as TEntity;
        }

        public TEntity FindMapEntity<TEntity>(Predicate<TEntity> predicate) where TEntity : WorldEntity
        {
            return worldEntities.Find(entity => { TEntity target = entity as TEntity; return target != null && predicate(target); }) as TEntity;
        }
    }
}