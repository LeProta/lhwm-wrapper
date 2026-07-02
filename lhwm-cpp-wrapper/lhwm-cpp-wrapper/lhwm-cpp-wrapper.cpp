#include "pch.h"
#include "lhwm-cpp-wrapper.h"
#include <msclr\marshal_cppstd.h>
#using <mscorlib.dll>
#using <system.dll>

#using "lhwm-wrapper.dll"

using namespace LHWMWrapperNameSpace;
using namespace System::Collections::Generic;

std::map<std::string, std::vector<std::tuple<std::string, std::string, std::string>>> LHWM::GetHardwareSensorMap()
{
	msclr::interop::marshal_context context;

	System::Collections::Generic::Dictionary<System::String^, System::Collections::Generic::List<System::Tuple<System::String^, System::String^, System::String^>^>^>^ map =
		LHWMWrapperNameSpace::LHWMWrapper::GetInstance()->GetHardwareSensorMap();
	
	std::map<std::string, std::vector<std::tuple<std::string, std::string, std::string>>> ret;

	for each (KeyValuePair<System::String^, System::Collections::Generic::List<System::Tuple<System::String^, System::String^, System::String^>^>^> ^ pair in map)
	{
		std::string hardware_name = context.marshal_as<std::string>(pair->Key);		
		std::vector<std::tuple<std::string, std::string, std::string>> list;

		for (int i = 0; i < pair->Value->Count; i++)
		{
			System::Tuple<System::String^, System::String^, System::String^>^ tup = pair->Value[i];

			list.push_back({
				context.marshal_as<std::string>(tup->Item1),
				context.marshal_as<std::string>(tup->Item2),
				context.marshal_as<std::string>(tup->Item3)
			});

		}

		ret[hardware_name] = list;

	}

	return ret;
}

float LHWM::GetSensorValue(std::string identifier)
{
	return LHWMWrapperNameSpace::LHWMWrapper::GetInstance()->GetSensorValue(gcnew System::String(identifier.c_str()));
}

void LHWM::SetControlValue(std::string identifier, float value)
{
	LHWMWrapperNameSpace::LHWMWrapper::GetInstance()->SetControlValue(gcnew System::String(identifier.c_str()), value);
}

void LHWM::SetControlDefault(std::string identifier)
{
	LHWMWrapperNameSpace::LHWMWrapper::GetInstance()->SetControlDefault(gcnew System::String(identifier.c_str()));
}

std::string LHWM::GetReport()
{
	msclr::interop::marshal_context context;
	return context.marshal_as<std::string>(
		LHWMWrapperNameSpace::LHWMWrapper::GetInstance()->GetReport());
}