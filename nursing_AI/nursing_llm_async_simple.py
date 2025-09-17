from dotenv import load_dotenv
import os, re, json, asyncio
from typing import Dict, Any
from langchain_community.document_loaders import Docx2txtLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_openai import ChatOpenAI
from langchain_upstage import UpstageEmbeddings
from pinecone import Pinecone
from langchain_pinecone import PineconeVectorStore
from langchain.prompts import ChatPromptTemplate, MessagesPlaceholder, FewShotChatMessagePromptTemplate
from langchain.chains import create_history_aware_retriever, create_retrieval_chain
from langchain.chains.combine_documents import create_stuff_documents_chain
from langchain_community.chat_message_histories import ChatMessageHistory
from langchain_core.chat_history import BaseChatMessageHistory
from langchain_core.runnables.history import RunnableWithMessageHistory
from langchain_core.output_parsers import StrOutputParser

try:
    from .config import answer_examples
except ImportError:
    from config import answer_examples 

load_dotenv()

# ========================================
# 🔄 기존 동기 함수들의 비동기 버전 (스트리밍 제거)
# ========================================

store = {}

def get_session_history(session_id: str) -> BaseChatMessageHistory:
    if session_id not in store:
        store[session_id] = ChatMessageHistory()
    return store[session_id]

async def get_retriever():
    """비동기 리트리버 생성"""
    embedding = UpstageEmbeddings(model="solar-embedding-1-large")
    index_name = os.getenv("PINECONE_INDEX", "nursing-upsate-index")
    pinecone_api_key = os.getenv("PINECONE_API_KEY")
    Pinecone(api_key=pinecone_api_key)
    vectorstore = PineconeVectorStore.from_existing_index(index_name=index_name, embedding=embedding)
    return vectorstore.as_retriever(search_kwargs={"k": 3})

def get_llm_async():
    """비동기 LLM (스트리밍 없음)"""
    openai_api_key = os.getenv("OPENAI_API_KEY")
    return ChatOpenAI(
        model="gpt-4o",
        openai_api_key=openai_api_key,
        temperature=0.1
    )

async def get_rag_chain_async():
    """⚡ 비동기 RAG 체인 (스트리밍 제거)"""
    llm = get_llm_async()
    retriever = await get_retriever()
    
    # Few-shot 프롬프트
    example_prompt = ChatPromptTemplate.from_messages([
        ("human", "{input}"),
        ("ai", "{answer}"),
    ])
    few_shot_prompt = FewShotChatMessagePromptTemplate(
        example_prompt=example_prompt,
        examples=answer_examples,
    )

    system_prompt = (
        "당신은 간호학 실습을 도와주는 시뮬레이션 AI입니다.\n"
        "답변시 마크다운 형식이 아닌 텍스트 형식으로 답변해주세요.\n"
        "약품 용기에 관한 문제는 Answer와 UserAnswer를 비교해서 정답을 평가합니다.\n"
        "각 항목에서 사용자의 답이 정답인지 확인하고,\n"
        "정답일 경우 '정답: 정확하게 확인하였습니다. 다음 단계로 이동하세요.'\n"
        "오답일 경우 '오답: 피드백을 제공합니다.'\n"
        "손 씻기 평가 시, 손바닥 → 손등 → 손가락 사이 → 두손 모아 → 엄지손가락 → 손톱 밑의 순서를 기준으로 평가합니다.\n"
        "준비물 평가는 총 7개 항목(알코올 솜, 손 소독제, 주사기, 트레이, 의료폐기물박스, 손상성 폐기물 박스, 투약 카드)이 모두 포함되어야 정답입니다.\n"
        "모든 출력은 반드시 **다음 JSON 포맷**을 따라야 합니다:\n"
        "\n"
        "{{\n"
        "  \"correct_count\": [정답 개수],\n"
        "  \"incorrect_count\": [오답 개수],\n"
        "  \"total_questions\": [전체 문항 수],\n"
        "  \"score_percentage\": [정답 비율 0~100 소수점 1자리까지],\n"
        "  \"feedback\": \"각 문항에 대한 평가 내용을 여기에 자연어로 작성합니다.\"\n"
        "}}\n"        
        "feedback은 Q(문제번호). (정답 또는 오답) : 피드백 내용 형식으로 작성해주세요.\n"
        "\n참고 문서:\n{context}"
    )

    qa_prompt = ChatPromptTemplate.from_messages([
        ("system", system_prompt),
        few_shot_prompt,
        MessagesPlaceholder("chat_history"),
        ("human", "{input}"),
    ])

    # 히스토리 인식 리트리버
    contextualize_q_prompt = ChatPromptTemplate.from_messages([
        ("system", "이전 대화 내용을 바탕으로 사용자의 질문을 독립적인 질문으로 변환해주세요."),
        MessagesPlaceholder("chat_history"),
        ("human", "{input}"),
    ])
    history_aware_retriever = create_history_aware_retriever(llm, retriever, contextualize_q_prompt)
    
    question_answer_chain = create_stuff_documents_chain(llm, qa_prompt)
    rag_chain = create_retrieval_chain(history_aware_retriever, question_answer_chain)

    return RunnableWithMessageHistory(
        rag_chain,
        get_session_history,
        input_messages_key="input",
        history_messages_key="chat_history",
        output_messages_key="answer",
    ).pick("answer")

# ========================================
# 🚀 비동기 응답 함수들 (스트리밍 제거)
# ========================================

async def get_ai_response_async(user_message: str, session_id: str = "nursing-session") -> Dict[str, Any]:
    """⚡ 비동기 AI 응답 (기존 호환성 유지)"""
    try:
        rag_chain = await get_rag_chain_async()
        
        # 비동기로 응답 생성
        response_text = await rag_chain.ainvoke(
            {"input": user_message},
            config={"configurable": {"session_id": session_id}}
        )
        
        # JSON 파싱 시도
        try:
            parsed = json.loads(response_text)
            parsed["answer"] = parsed.pop("feedback", response_text)
            return parsed
        except json.JSONDecodeError:
            # 파싱 실패 시 fallback
            counts = count_answer_results(response_text)
            return {
                "answer": response_text,
                **counts
            }
            
    except Exception as e:
        return {
            "answer": f"처리 중 오류가 발생했습니다: {str(e)}",
            "correct_count": 0,
            "incorrect_count": 0,
            "total_questions": 0,
            "score_percentage": 0.0
        }

async def get_followup_question_async(user_answer: str, missing_keywords: list[str]) -> str:
    """🔄 비동기 후속 질문 생성"""
    try:
        llm = get_llm_async()
        
        prompt_text = (
            "당신은 어린 아이를 둔 보호자입니다. 간호사의 설명에서 중요한 정보가 누락되어 걱정됩니다.\n"
            f"간호사 설명: {user_answer}\n"
            f"누락된 키워드: {', '.join(missing_keywords)}\n\n"
            "보호자 입장에서 자연스럽고 걱정스러운 톤으로 후속 질문을 1-2문장으로 작성해주세요."
        )
        
        prompt = ChatPromptTemplate.from_template(prompt_text)
        chain = prompt | llm | StrOutputParser()
        
        result = await chain.ainvoke({
            "user_answer": user_answer,
            "missing_keywords": missing_keywords
        })
        
        return result.strip()
        
    except Exception as e:
        return f"더 자세한 설명이 필요해요. ({', '.join(missing_keywords)} 관련해서)"

# ========================================
# 🛠️ 유틸리티 함수들
# ========================================

def count_answer_results(text: str) -> Dict[str, Any]:
    """텍스트에서 정답/오답 개수 계산"""
    correct_matches = re.findall(r"정답\s*:", text, flags=re.MULTILINE)
    incorrect_matches = re.findall(r"오답\s*:", text, flags=re.MULTILINE)

    correct_count = len(correct_matches)
    incorrect_count = len(incorrect_matches)
    total_questions = correct_count + incorrect_count
    score_percentage = round((correct_count / total_questions * 100) if total_questions > 0 else 0, 1)

    return {
        "correct_count": correct_count,
        "incorrect_count": incorrect_count,
        "total_questions": total_questions,
        "score_percentage": score_percentage
    }

# ========================================
# 🧪 테스트 함수
# ========================================

async def test_async_performance():
    """비동기 성능 테스트"""
    import time
    
    print("⚡ 비동기 처리 성능 테스트 시작...")
    
    test_question = """
    Q1. "약물 이름은 무엇인가요?"
    사용자 선택: 1) Cefotaxime
    
    Q2. "투약 경로는 무엇인가요?"
    사용자 선택: 2) 정맥투여
    """
    
    start_time = time.time()
    result = await get_ai_response_async(test_question)
    end_time = time.time()
    
    print(f"📊 응답 시간: {end_time - start_time:.2f}초")
    print(f"💬 응답 길이: {len(result['answer'])} 문자")
    print(f"✅ 정답: {result['correct_count']}개")
    print(f"❌ 오답: {result['incorrect_count']}개")
    print(f"🎯 점수: {result['score_percentage']}%")

if __name__ == "__main__":
    asyncio.run(test_async_performance())
