//this file is generated by codedump tool at 2023/11/21 16:30:49 do NOT edit it !
#pragma once

#include <map>
#include <vector>
#include <string>
#include <bulk_reader.h>



struct SkillContainer
{
    std::map<std::string,SkillData> mapDesignData; 
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


class skill_containerParser : public ConfReader<skill_containerParser>
{
public:
    bool Initialize(const char* file);
    virtual bool LoadConfig(nlohmann::json& root, bool init);
    const SkillContainer& GetConfig(){return m_config;}
private:
    bool ParseSkillContainer(nlohmann::json& root, SkillContainer& info);
    bool ParseSkillData(nlohmann::json& root, SkillData& info);
    bool ParseSkillEffectInfo(nlohmann::json& root, SkillEffectInfo& info);
private:
    SkillContainer m_config;
};

