from dotenv import load_dotenv
import os

from langchain_community.document_loaders import Docx2txtLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_openai import ChatOpenAI
from langchain_upstage import UpstageEmbeddings
from pinecone import Pinecone
from langchain_pinecone import PineconeVectorStore
from langchain_core.chat_history import BaseChatMessageHistory
from langchain_community.chat_message_histories import ChatMessageHistory
from langchain_core.runnables.history import RunnableWithMessageHistory

load_dotenv()

store = {}

def get_session_history(session_id: str) -> BaseChatMessageHistory:
    if session_id not in store:
        store[session_id] = ChatMessageHistory()
    return store[session_id]

def get_llm(model="gpt-4.1"):
    openai_api_key = os.getenv("OPENAI_API_KEY")
    return ChatOpenAI(model=model, openai_api_key=openai_api_key)

# 정답지
correct_answers = {
    "Q1": "Cefotaxime 0.5g Vial",
    "Q2": "정맥투여 (IV)",
    "Q3": "1kg 당 50mg을 하루 8시간 간격으로 투여",
    "Q4": "5:40am (정규 6am 기준 20분 전)",
    "Q5": "세팔로스포린계 항생물질",
    "Q6": "정맥주사 또는 근육주사",
    "Q7": "예 반드시 희석 후 투여해야 합니다.",
    "Q8": "적합, 약물의 유효기간은 정상입니다.",
    "Q9": "적합, 용기가 개봉되지 않고 밀봉이 되어있으며 이상 여부 없습니다.",
    "Q10": "460mg",
    "Q11": "0.9% 생리식염수",
    "Q12": "2ml",
    "Q13": "1.84ml",
    "Q14": "6단계 손 위생 수행 (물 사용)",
    "Q15": "1, 2, 3, 4, 5, 6",
    "Q16": "약물명, 1회 투여 용량, 투여시간",
    "Q17": "이하트, 239845",
    "Q18": "주사의 목적과 과정(감염 치료, 수액줄로 천천히 주입 등)을 상세히 설명",
    "Q19": "손바닥, 손등, 손가락 사이, 두 손 모아, 엄지손가락, 손톱 밑",
    "Q20": "2) 비정상, IV route 교체 필요",
    "Q21": "바늘을 바로 손상성 폐기물 박스에 폐기",
    "Q22": "비정상, 이상 반응 발생",
    "Q23": "주입 중단, 보고",
    "Q24": "부작용 발생 시 대응 내용 정확히 전달"
}

def compare_with_llm_feedback(user_answers: dict) -> dict:
    llm = get_llm()
    feedback_result = {}
    for q_num, user_answer in user_answers.items():
        correct = correct_answers.get(q_num)
        if user_answer.strip() == correct.strip():
            feedback_result[q_num] = {
                "result": "정답",
                "feedback": "✅ 정확하게 확인하였습니다. 다음 단계로 이동하세요."
            }
        else:
            prompt = f"왜 사용자의 답변 '{user_answer}'는 정답 '{correct}'와 다른가요?  간결하고 명확한 이유를 피드백 형태로 말해줘."
            reason = llm.invoke(prompt).content
            feedback_result[q_num] = {
                "result": "오답",
                "feedback": f"❌ 정답은 '{correct}'입니다. {reason}"
            }
    return feedback_result

# 사용 예시
def get_mixed_eval_response(user_answer_dict):
    return compare_with_llm_feedback(user_answer_dict)

# 사용하려면 아래처럼 호출
result = get_mixed_eval_response({"Q1": "Cefotaxime 0.5g Vial"})
print(result["Q1"])
