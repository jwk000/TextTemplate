//this file is generated by codedump tool at 3/22/2019 13:47:48 do NOT edit it !
#pragma once

#include <map>
#include <vector>
#include <string>
#include <bulk_reader.h>

struct SkillContainer
{
    std::map<std::string,SkillData> mapDesignData; 
    std::map<std::string,EffectData> mapEffect; 
    std::map<std::string,BuffData> mapBuff; 
    std::map<std::string,ProjectileData> mapProjectile; 
    std::map<std::string,SupportSkillData> mapSupportSkill; 
};


struct SkillData
{
    std::string id; 
    std::string name; 
    std::string desc; 
    std::string descSpecial; 
    std::string icon; 
    int memorySlot; //记忆槽
    int actionPt; //行动点
    float cd; 
    std::string resistedBy; //被何种护甲抵抗
    float range; 
    std::string category; 
    std::vector<SkillEffectInfo> effList; 
};


struct SkillEffectInfo
{
    std::string effId; 
    std::string effType; 
    std::string locateType; 
    float targetPosOffsetX; 
    float targetPosOffsetY; 
    float targetPosOffsetZ; 
};


struct EffectData
{
    std::string id; 
    std::string name; 
    std::string desc; 
    std::string elementType; 
    ComTargeting comTargeting; 
    ComEvaluate comEvaluate; 
    ComTrigger comTrigger; 
    ComRender comRender; 
    ComDisplace comDisplace; 
};


struct ComTargeting
{
    std::string refSubject; 
    std::string shape; 
    std::string target; 
    std::map<std::string,float> mapParams; 
};


struct ComEvaluate
{
    std::string minExpression; 
    std::string maxExpression; 
};


struct ComTrigger
{
    std::vector<ComTriggerEntry> entries; 
};


struct ComTriggerEntry
{
    std::string timing; 
    std::string type; 
    std::string triggerTargetId; 
    std::string targetAffected; 
    std::map<std::string,std::string> triggerParams; 
};


struct ComRender
{
    std::string renderEntityID; 
};


struct ComDisplace
{
    std::string subject; 
    std::string type; 
    std::map<std::string,float> mapParams; 
};


struct BuffData
{
    std::string id; 
    std::string name; 
    std::string desc; 
    std::string status; 
    std::string removeStatus; 
    std::vector<BuffComGroup> comGroups; 
    ComTrigger comTrigger; 
    ComRender comRender; 
    ComRemove comRemove; 
};


struct ComRemove
{
    std::string refSubject; 
    std::string removeTiming; 
    std::map<std::string,float> mapParams; 
    float forceRemoveTime; 
};


struct BuffComGroup
{
    ComEvaluate comEvaluate; 
    ComActivate comActivate; 
};


struct ComActivate
{
    std::string activateType; 
    std::map<std::string,float> mapParams; 
};


struct ComReplace
{
    std::string targetType; 
    std::string targetId; 
};


struct SupportSkillData
{
    std::string id; 
    std::string name; 
    std::string desc; 
    std::string elementType; 
    ComEvaluate comEvaluate; 
    ComActivate comActivate; 
    ComReplace comReplace; 
};


struct ProjectileData
{
    std::string id; 
    std::string name; 
    std::string desc; 
    std::string elementType; 
    ComTargeting comTargeting; 
    ComEvaluate comEvaluate; 
    ComTrigger comTrigger; 
    ComRender comRender; 
    ComDisplace comDisplace; 
    ComRemove comRemove; 
};


class SkillContainerParser : public ConfReader<SkillContainerParser>
{
public:
    bool Init(const char* file);
    virtual bool LoadConfig(nlohmann::json& root);
    const SkillContainer& GetConfig(){return m_config;}
private:
    bool ParseSkillContainer(nlohmann::json& root, SkillContainer& info);
    bool ParseSkillData(nlohmann::json& root, SkillData& info);
    bool ParseSkillEffectInfo(nlohmann::json& root, SkillEffectInfo& info);
    bool ParseEffectData(nlohmann::json& root, EffectData& info);
    bool ParseComTargeting(nlohmann::json& root, ComTargeting& info);
    bool ParseComEvaluate(nlohmann::json& root, ComEvaluate& info);
    bool ParseComTrigger(nlohmann::json& root, ComTrigger& info);
    bool ParseComTriggerEntry(nlohmann::json& root, ComTriggerEntry& info);
    bool ParseComRender(nlohmann::json& root, ComRender& info);
    bool ParseComDisplace(nlohmann::json& root, ComDisplace& info);
    bool ParseBuffData(nlohmann::json& root, BuffData& info);
    bool ParseComRemove(nlohmann::json& root, ComRemove& info);
    bool ParseBuffComGroup(nlohmann::json& root, BuffComGroup& info);
    bool ParseComActivate(nlohmann::json& root, ComActivate& info);
    bool ParseComReplace(nlohmann::json& root, ComReplace& info);
    bool ParseSupportSkillData(nlohmann::json& root, SupportSkillData& info);
    bool ParseProjectileData(nlohmann::json& root, ProjectileData& info);
private:
    SkillContainer m_config;
};

