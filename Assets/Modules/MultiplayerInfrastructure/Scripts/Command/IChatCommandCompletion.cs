using System.Collections.Generic;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// 명령어가 Tab 키 자동완성 후보를 직접 제공할 수 있게 하는 선택적 인터페이스.
  /// <para>
  /// 구현하지 않은 명령어도 Usage 정의에 기반한 하위 명령어·일반 인수
  /// 자동완성을 지원합니다. 이 인터페이스는 런타임 데이터나 복잡한
  /// 맥락처럼 Usage만으로 표현할 수 없는 후보를 제공할 때 사용합니다.
  /// </para>
  /// </summary>
  public interface IChatCommandCompletion
  {
    /// <summary>
    /// 주어진 인수 위치에 대한 자동완성 후보 목록을 반환합니다.
    /// </summary>
    /// <param name="argIndex">
    /// 명령어 이름을 제외한 인수들의 0-based 인덱스.
    /// 예: <c>/give endo</c> 에서 "endo"는 argIndex 0.
    /// </param>
    /// <param name="previousArgs">
    /// 현재 위치 이전에 이미 입력된 인수들.
    /// 예: <c>/tag add Player1 </c> 에서 <c>["add", "Player1"]</c>.
    /// </param>
    /// <param name="partial">
    /// 현재 입력 중인 부분 텍스트. 빈 문자열일 수 있습니다.
    /// </param>
    /// <returns>자동완성 후보 문자열 목록. 없으면 빈 목록.</returns>
    public IReadOnlyList<string> GetCompletions(int argIndex, string[] previousArgs, string partial);
  }
}
