"""Regression checks for scenario interaction data after content-branch merges.

Run from any directory: python3 Tools/scenario-json-validator/test_interaction_migration.py
"""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCENARIOS = ROOT / 'Assets/Modules/TriageTrainer/Resources/Scenario'


def load(name):
    return json.loads((SCENARIOS / f'{name}.scenario.json').read_text(encoding='utf-8-sig'))


def address(definition):
    return (tuple(sorted(definition['entity'].items())), definition['interaction'])


class InteractionMigrationTests(unittest.TestCase):
    def test_no_retired_interaction_formats_in_any_scenario(self):
        for path in (ROOT / 'Assets/Modules').rglob('*.scenario.json'):
            with self.subTest(path=path.relative_to(ROOT)):
                data = json.loads(path.read_text(encoding='utf-8-sig'))
                for npc in data.get('actingNpcs', []):
                    self.assertNotIn('interactions', npc)
                for node in data['nodes'].values():
                    self.assertNotIn(node['nodeType'],
                                     ('ItemSubmissionConfig', 'NpcInteractControl', 'Interaction'))
                    if node['nodeType'] == 'NPCControl':
                        for field in ('interactOperation', 'interactableIdentifier',
                                      'interactEnabled', 'resultStateKey'):
                            self.assertNotIn(field, node)

    def test_scenario_definitions_and_visibility_targets_survive_content_merges(self):
        for name, count in [('patient_a_critical', 35), ('patient_b_c_ct', 18),
                            ('disaster_intro', 3), ('tutorial', 3)]:
            with self.subTest(scenario=name):
                data = load(name)
                definitions = data['interactions']
                self.assertEqual(len(definitions), count)
                addresses = {address(value) for value in definitions}
                self.assertEqual(len(addresses), count, 'Duplicate interaction address')
                for node in data['nodes'].values():
                    if node['nodeType'] == 'InteractionVisibility':
                        for target in node['targets']:
                            self.assertIn(address(target), addresses)
                for definition in definitions:
                    self.assertIn('visibility', definition)
                    self.assertNotIn('consumeOnce', definition)
                    self.assertNotIn('enabled', definition)

    def test_doctor_submission_keeps_items_signals_and_visibility_sequence(self):
        data = load('patient_a_critical')
        expected = [
            ('LARYNGOSCOPE', 'laryngoscope', 'laryngoscope', 'pass_laryngoscope', 'V014_1'),
            ('ET_TUBE', 'et-tube', 'endotracheal_tube_ready', 'pass_et_tube_ready', 'V014_2'),
            ('SYRINGE', '5cc-syringe', 'syringe_5cc', 'pass_syringe', 'V014_4'),
            ('CENTRAL_LINE_SET', 'central-line-set', 'central_line_set',
             'pass_central_line_set', 'V018'),
        ]
        for suffix, interaction, item, signal, next_node in expected:
            with self.subTest(submission=suffix):
                definition = next(value for value in data['interactions']
                                  if value['interaction'] == f'patient-a-doctor-submit-{interaction}')
                node = data['nodes'][f'ISC_PASS_{suffix}']
                self.assertEqual(definition['entity'], {'id': 'npc-doctor-patient-a-critical'})
                self.assertEqual(definition['kind'], 'ItemSubmission')
                self.assertEqual(definition['itemSubmission']['requiredItems'],
                                 [{'itemIdentifier': item, 'count': 1}])
                self.assertEqual(definition['completionSignal'], signal)
                self.assertEqual(definition['afterInteract'], 'HideForAll')
                self.assertFalse(definition['visibility']['initial'])
                self.assertEqual(node['nodeType'], 'InteractionVisibility')
                self.assertEqual(node['operation'], 'Show')
                self.assertEqual(node['nextIdentifier'], next_node)
                self.assertEqual(address(node['targets'][0]), address(definition))

    def test_intro_opens_transfer_after_prompt_and_before_signal_wait(self):
        data = load('disaster_intro')
        prompt = data['nodes']['N001_5']
        show = data['nodes'][prompt['nextIdentifier']]
        self.assertEqual(show['nodeType'], 'InteractionVisibility')
        self.assertEqual(show['operation'], 'Show')
        self.assertEqual(show['targets'], [{'entity': {'id': 'patient_a'},
                                           'interaction': 'transfer_prompt'}])
        self.assertEqual(show['nextIdentifier'], 'V004')
        definition = next(value for value in data['interactions']
                          if value['interaction'] == 'transfer_prompt')
        self.assertFalse(definition['visibility']['initial'])
        self.assertEqual(definition['completionSignal'], 'move_patient_a')

    def test_intro_final_arrival_uses_current_position_and_waits_for_each_role(self):
        nodes = load('disaster_intro')['nodes']
        parallel = nodes[nodes['D004']['nextIdentifier']]
        self.assertEqual(parallel['nodeType'], 'Parallel')
        self.assertEqual(parallel['waitMode'], 'All')
        self.assertEqual(parallel['allocationType'], 'ByRole')
        branches = parallel['branches']
        self.assertEqual([branch['requiredPlayerTags'] for branch in branches],
                         [['nurse_b'], ['nurse_c'], ['nurse_d']])
        self.assertEqual(len({branch['identifier'] for branch in branches}), 3)
        self.assertEqual(len({branch['completionConditionIdentifier'] for branch in branches}), 3)
        # A single player can hold multiple roles; removing one quest must not erase another gate's state.
        self.assertEqual(len({nodes[branch['identifier']]['quest']['Id'] for branch in branches}), 3)

        payload = json.loads((ROOT / 'Assets/Modules/TriageTrainer/Resources/Quest'
                              / 'disaster_intro.quests.quest.json').read_text())
        definitions = {f"{payload['namespace']}::{q['identifier']}": q
                       for q in payload['definitions']}
        scene = (ROOT / 'Assets/Scenes/OverworldScene.unity').read_text()
        for branch in branches:
            with self.subTest(role=branch['requiredPlayerTags']):
                add = nodes[branch['identifier']]
                quest_id = add['quest']['Id']
                definition = definitions[add['questDefinitionIdentifier']]
                self.assertEqual(add['operation'], 'Add')
                self.assertEqual(definition['scope'], 'Player')
                self.assertTrue(definition['isAutoComplete'])
                self.assertTrue(definition['isTrackedByDefault'])
                criterion, = definition['completionCriteria']
                # A signal left over from the initial triage visit must not complete this quest.
                self.assertEqual(criterion['type'], 'WaypointReached')
                self.assertGreater(criterion['reachDistance'], 0)
                self.assertIn(f"  identifier: {criterion['waypointIdentifier']}\n", scene)
                gate = nodes[add['nextIdentifier']]
                rule, = gate['rootConditions'][0]['validationRules']
                self.assertEqual(rule['registryIdentifier'], f'quest.completed.{quest_id}')
                self.assertTrue(gate['waitForCondition'])
                remove = nodes[gate['nextIdentifier']]
                self.assertEqual(remove['operation'], 'Remove')
                self.assertEqual(remove['quest']['Id'], quest_id)
                self.assertEqual(remove['questDefinitionIdentifier'], add['questDefinitionIdentifier'])
                self.assertEqual(remove['nextIdentifier'], branch['completionConditionIdentifier'])
                self.assertIn(remove['nextIdentifier'], nodes)

        event = nodes[parallel['nextIdentifier']]
        self.assertEqual(event['eventIdentifier'], 'B_C_D_to_triage')
        ending = nodes[event['nextIdentifier']]
        self.assertEqual(ending['nodeType'], 'Dialogue')
        self.assertIsNone(ending['nextIdentifier'])
        self.assertNotIn('다음 처치 시나리오', ending['dialogueContent'])

    def test_bc_visibility_roles_follow_current_patient_branches(self):
        data = load('patient_b_c_ct')
        expected_ids = {'recognition_1', 'recognition_2', 'recognition_3', 'recognition_4',
                        'strength_check', 'pupil_check', 'patient_bc_nasal_cannula',
                        'intravenous_line_cannula', 'normal_saline_connect'}
        for patient, prefix in [('patient_b', 'B'), ('patient_c', 'C')]:
            definitions = [value for value in data['interactions']
                           if value['entity'] == {'id': patient}]
            self.assertEqual({value['interaction'] for value in definitions}, expected_ids)
            for definition in definitions:
                interaction = definition['interaction']
                initial = interaction.startswith('recognition_') or interaction == 'strength_check'
                parallel = data['nodes']['P_B_C_CARE' if initial else 'P_B_C_TREATMENT']
                branch_id = ('A_RECOG_Q' if prefix == 'B' else 'C_A_RECOG_Q') if initial else (
                    ('D_OXY_Q' if prefix == 'B' else 'C_D_OXY_Q')
                    if interaction == 'patient_bc_nasal_cannula' else
                    ('C_PUPIL_Q' if prefix == 'B' else 'C_C_PUPIL_Q'))
                branch = next(value for value in parallel['branches'] if value['identifier'] == branch_id)
                self.assertEqual(definition['visibility']['conditions'],
                                 [{'type': 'PlayerHasTag', 'tag': branch['requiredPlayerTags'][0]}])

    def test_quest_visibility_references_existing_quests_and_objectives(self):
        quests = {}
        for path in (ROOT / 'Assets/Modules').rglob('*.quest.json'):
            data = json.loads(path.read_text(encoding='utf-8-sig'))
            for definition in data.get('definitions', []):
                quests[definition['identifier']] = definition

        def conditions(values):
            for condition in values:
                yield condition
                yield from conditions(condition.get('conditions', []))

        def identifiers(value):
            if isinstance(value, dict):
                if 'identifier' in value:
                    yield value['identifier']
                for child in value.values():
                    yield from identifiers(child)
            elif isinstance(value, list):
                for child in value:
                    yield from identifiers(child)

        for path in SCENARIOS.glob('*.scenario.json'):
            data = json.loads(path.read_text(encoding='utf-8-sig'))
            for definition in data.get('interactions', []):
                for condition in conditions(definition.get('visibility', {}).get('conditions', [])):
                    if condition['type'] != 'PlayerHasQuest':
                        continue
                    quest = condition['questIdentifier']
                    with self.subTest(scenario=path.name, interaction=definition['interaction'], quest=quest):
                        self.assertIn(quest, quests)
                        objective = condition.get('completionCriteriaIdentifier')
                        if objective:
                            self.assertIn(objective, set(identifiers(quests[quest])))


if __name__ == '__main__':
    unittest.main()
