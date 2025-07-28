from dotenv import load_dotenv
import os

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

store = {}

def get_session_history(session_id: str) -> BaseChatMessageHistory:
    if session_id not in store:
        store[session_id] = ChatMessageHistory()
    return store[session_id]

def load_doc_from_path(path: str):
    docs = Docx2txtLoader(path).load_and_split(
        text_splitter=RecursiveCharacterTextSplitter(chunk_size=1000, chunk_overlap=200)
    )
    return docs

def get_retriever():
    embedding = UpstageEmbeddings(model="solar-embedding-1-large")
    index_name = os.getenv("PINECONE_INDEX", "nursing-upsate-index")
    pinecone_api_key = os.getenv("PINECONE_API_KEY")
    Pinecone(api_key=pinecone_api_key)
    vectorstore = PineconeVectorStore.from_existing_index(index_name=index_name, embedding=embedding)
    return vectorstore.as_retriever(search_kwargs={"k": 3})

def get_llm(model="gpt-4.1"):
    openai_api_key = os.getenv("OPENAI_API_KEY")
    return ChatOpenAI(model=model, openai_api_key=openai_api_key)

def get_history_retriever():
    llm = get_llm()
    retriever = get_retriever()
    contextualize_q_prompt = ChatPromptTemplate.from_messages([
        ("system", "이전 대화 내용을 바탕으로 사용자의 질문을 독립적인 질문으로 변환해주세요."),
        MessagesPlaceholder("chat_history"),
        ("human", "{input}"),
    ])
    return create_history_aware_retriever(llm, retriever, contextualize_q_prompt)

def get_rag_chain():
    llm = get_llm()
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
        "약품 용기에 관한 문제는 Answer와 UserAnswer를 비교해서 정답을 평가합니다. 이때 8번과 9번 문제에 대한 답변이 함께 제공됩니다.\n"
        "8번 9번은 질문에 대한 답변만 잘 되어 있으면 정답으로 처리합니다"
        "각 항목에서 사용자의 답이 정답인지 확인하고,\n"
        "정답일 경우  '정답: 정확하게 확인하였습니다. 다음 단계로 이동하세요.'\n"
        "오답일 경우  '오답: 피드백을 제공합니다.'\n"
        "손 씻기 평가 시, 손바닥 → 손등 → 손가락 사이 → 두손 모아 → 엄지손가락 → 손톱 밑의 순서를 기준으로 평가합니다.\n"
        "준비물 평가는 총 6개 항목(주사기, 알콜솜, tray, 손 소독제, 손상성 폐기물박스, 감염성 폐기물박스)이 모두 포함되어야 정답입니다.\n"
        "\n참고 문서:\n{context}"
    )

    qa_prompt = ChatPromptTemplate.from_messages([
        ("system", system_prompt),
        few_shot_prompt,
        MessagesPlaceholder("chat_history"),
        ("human", "{input}"),
    ])

    history_aware_retriever = get_history_retriever()
    question_answer_chain = create_stuff_documents_chain(llm, qa_prompt)
    rag_chain = create_retrieval_chain(history_aware_retriever, question_answer_chain)

    conversational_rag_chain = RunnableWithMessageHistory(
        rag_chain,
        get_session_history,
        input_messages_key="input",
        history_messages_key="chat_history",
        output_messages_key="answer",
    ).pick("answer")

    return conversational_rag_chain

def get_ai_response(user_message):
    rag_chain = get_rag_chain()
    response = rag_chain.invoke(
        {"input": user_message},
        config={"configurable": {"session_id": "nursing-session"}}
    )
    
    # 정답과 오답 개수 계산
    correct_count = response.count("✅")
    incorrect_count = response.count("❌")
    total_questions = correct_count + incorrect_count
    
    # 점수 계산
    score_percentage = round((correct_count / total_questions * 100) if total_questions > 0 else 0, 1)
    
    # 결과를 딕셔너리로 반환
    result = {
        "answer": response,
        "correct_count": correct_count,
        "incorrect_count": incorrect_count,
        "total_questions": total_questions,
        "score_percentage": score_percentage
    }
    
    return result

if __name__ == "__main__":
    test_message = """
    Q10. "체중 9.2kg, 50mg/kg/day q 8hr 처방 시, 1회 용량은 얼마인가요?"  
    사용자 선택: 500mg

    Q11. 처방을 참고하여 "Cefotaxime은 어떤 용액에 희석해야 하나요?"  
    사용자 선택: 1) 증류수 (Water for Injection)

    Q12. "희석 시 필요한 용량은?"  
    사용자 선택: 2) 2.5ml

    Q13. "Cefotaxime 500mg에 N/S 2cc 희석 후 처방된 1회 주입 용량을 계산하면?"  
    사용자 선택: 1.84ml    
    """
    result = get_ai_response(test_message)
    print("AI 답변:\n", result["answer"])
    print(f"📊 정답: {result['correct_count']}개, 오답: {result['incorrect_count']}개, 총 문제: {result['total_questions']}개")
    print(f"🎯 점수: {result['score_percentage']}%")
    
    # 객관식 문제 테스트
    print("\n" + "="*50)
    print("객관식 문제 테스트")
    print("="*50)
    
    objective_test = """
    Q1. "약물 이름은 무엇인가요?"
    선택지: 1) Cefotaxime 2) Cefazolin 3) Gentamicin 4) Ampicillin
    사용자 선택: 1) Cefotaxime
    
    Q2. "투약 경로는 무엇인가요?"
    선택지: 1) 근육주사 2) 정맥투여 3) 피하주사 4) 경구투여
    사용자 선택: 2) 정맥투여
    
    Q3. "1회 용량은 얼마인가요?"
    선택지: 1) 50mg 2) 100mg 3) 500mg 4) 1000mg
    사용자 선택: 3) 500mg
    """
    result2 = get_ai_response(objective_test)
    print("객관식 AI 답변:\n", result2["answer"])
    print(f"📊 정답: {result2['correct_count']}개, 오답: {result2['incorrect_count']}개, 총 문제: {result2['total_questions']}개")
    print(f"🎯 점수: {result2['score_percentage']}%")