meta:
  id: ctr_gameconfig
  application: Crash Team Racing
  title: Crash Team Racing (PS1) main game config struct in RAM
  file-extension: bin
  endian: le

doc: |
  kaitai-struct conversion of CTR RAM Mapping Project/ModSDK structs

seq:
  - id: game_mode
    type: u4
  - id: cheats
    type: u4
  - id: adv_flags
    type: u4
  - id: swapchain_index
    type: u4
  - id: ptr_swapchain
    type: u4
    repeat: expr
    repeat-expr: 2
  - id: db
    type: db
    repeat: expr
    repeat-expr: 2
  - id: ptr_lev1
    type: u4
  - id: prt_lev2
    type: u4
  - id: cameras
    type: camera
    repeat: expr
    repeat-expr: 4 
  - id: skip_array_12_entries
    size: 0x128
    repeat: expr
    repeat-expr: 12
  - id: camera_ui
    type: camera
  - id: driver_cameras
    type: camera_dc
    repeat: expr
    repeat-expr: 4
  - id: data_0xc0
    size: 0xC0
  - id: ptr_ot
    type: u4
    repeat: expr
    repeat-expr: 2
  - id: pools
    type: pool_struct
  - id: level_id
    type: u4
  - id: level_name
    type: strz
    size: 36
    encoding: ascii
  - id: skip_data20
    size: 0x20
  - id: data_0xc0_2
    size: 0xC0
  - id: ptr_clod
    type: u4
  - id: ptr_dustpuff
    type: u4
  - id: ptr_smoking
    type: u4
  - id: ptr_sparkle
    type: u4
  - id: ptr_icon_unk
    type: u4 
  - id: thread_buckets_array
    type: thread_buckets
  - id: ptr_render_bucket_instance
    type: u4
  - id: skip3
    size: 0x10
  - id: num_players
    type: u1
  - id: num_controllers
    type: u1
  - id: unk_unused
    type: u1
  - id: num_bots
    type: u1
  - id: data_between_screens_and_timer
    size: 0x30
  - id: unk_flip_positive_negative
    type: u4
  - id: unk_0_1_2
    type: u4
  - id: frame_timer_no_exceptions
    type: u4
  - id: frame_timer_not_paused
    type: u4
  - id: timer
    type: u4
  - id: various_timers
    type: u4
    repeat: expr
    repeat-expr: 7
  - id: traffic_lights_timer
    type: s2
  - id: unk_short_traffic_might_be_int
    type: s2
  - id: elapsed_event_time
    type: u4  
  - id: data_1c_always0
    size: 0x1C
  - id: unk_1d30
    type: u1
  - id: hud_flags
    type: u1
  - id: bool_demo_mode
    type: u1
  - id: num_laps
    type: u1
  - id: unk_timer_cooldown_similar_to_1d36 
    type: u2
  - id: timer_end_of_race_vs
    type: u2
  - id: cooldown_from_pause_until_unpause  
    type: u1
  - id: cooldown_from_unpause_until_pause
    type: u1
  - id: adv_pause_page
    type: u2
  - id: unk_related_to_relic_race
    type: u4
  - id: lap_index_new_best
    type: u4
  - id: unknown_flags_1d44
    type: u4
  - id: unknown_1d48_not_found
    type: u1
  - id: last_68_difference_sep3_usa_retail
    type: u1
  - id: not_found_in_code1
    type: u2
  - id: test
    type: u2    
  - id: not_found_in_code2 
    type: u2
  - id: not_found_in_code3
    type: u2
  - id: skip_0x24
    size: 0x24
  - id: unknown_notinsep3_again
    type: u2
  - id: time_to_beat_in_time_trial_for_current_event
    type: u4
  - id: skip5
    size: 8
  - id: original_event_time
    type: u4
  - id: battle_setup_struct
    type: battle_setup

instances:
  renderflags:
    pos: 0x256c
    type: u4
    doc: |
      0x256c - uint - render flags

      00000001 - draw lev
      00000002 - draw rain
      00000004 - ?
      00000008 - draw stars
      00000010 - ?
      00000020 - draw ctr models (instances?)
      00000040 - ?
      00000080 - probably wheels, but doesn't render without kart
      00000100 - ?
      00000200 - draw particles (fire, smoke)
      00000400 - draw shadow
      00000800 - draw heat effect
      00001000 - trigger checkered flag
      00002000 - clear back buffer with back color
      00004000 - ?
      00008000 - ?

      rest unknown or no visible effects

  ptr_drivers:
    pos: 0x24ec
    type: u4  
    repeat: expr
    repeat-expr: 8
  ptr_drivers_ordered:
    pos: 0x250C
    type: u4  
    repeat: expr
    repeat-expr: 8
  current_p1_standing:
    pos: 0x257a
    type: u1
    
types:

  battle_setup:
    seq:
      - id: life_limit
        type: u4
      - id: kill_limit
        type: u4
      - id: unk_related_to_team_squares
        type: u4
        repeat: expr
        repeat-expr: 4
      - id: weapons_enabled
        type: u4
      - id: player_teams
        type: u1
        repeat: expr
        repeat-expr: 4
      - id: skip
        size: 0x30
      - id: team_flags
        type: u4
      - id: num_teams
        type: u4
      - id: unk_weapons
        size: 0x40


  db:
    seq:
      - id: draw_env
        size: 0x5C
      - id: disp_env
        size: 0x14
      - id: unk_prim_mem_related
        type: u4
      - id: prim_mem
        type: primmem
      - id: ot_mem
        type: otmem

  primmem:
    seq:
      - id: size
        type: u4
      - id: ptr_start
        type: u4
      - id: ptr_end
        type: u4
      - id: ptr_curr
        type: u4
      - id: ptr_end_min_100
        type: u4 
      - id: unk1
        type: u4 	      
      - id: ptr_unk2
        type: u4 

  otmem:
    seq:
      - id: size
        type: u4
      - id: ptr_start
        type: u4
      - id: ptr_end
        type: u4
      - id: ptr_curr
        type: u4
      - id: ptr_start_plus_four
        type: u4

  camera:
    seq:
      - id: position
        type: vector3s
      - id: rotation
        type: vector3s
      - id: unk1
        type: vector3s
      - id: fade_in_current
        type: s2
      - id: fadein_target
        type: s2
      - id: fadein_step
        type: s2
      - id: unk3
        type: u4
      - id: vp_location
        type: vector2s
      - id: vp_size
        type: vector2s 
      - id: skip
        size: 0xD0         
      - id: ptr_ot
        type: u4
      - id: skip2
        size: 0x18

  camera_dc:
    seq:
      - id: driver_index
        type: u4
      - id: data_0x44
        size: 0x40
      - id: ptr_driver
        type: u4
      - id: ptr_camera
        type: u4      
      - id: data_0x90
        size: 0x90

  alloc_pool:
    seq:
      - id: ptr_last
        type: u4
      - id: ptr_first
        type: u4
      - id: num_entries
        type: u4
      - id: ptr_child_pool
        type: u4
      - id: unk1
        type: u4
      - id: unk2
        type: u4
      - id: num_entries_max
        type: u4
      - id: entry_size
        type: u4        
      - id: alloc_size
        type: u4
      - id: ptr_alloc
        type: u4 

  pool_struct:
    seq:
      - id: thread
        type: alloc_pool
      - id: instance
        type: alloc_pool
      - id: small_stack
        type: alloc_pool
      - id: medium_stack
        type: alloc_pool
      - id: large_stack
        type: alloc_pool
      - id: particle
        type: alloc_pool
      - id: oscillator
        type: alloc_pool
      - id: rain
        type: alloc_pool

  thread_bucket:
    seq:
      - id: ptr_thread
        type: u4
      - id: ptr_name
        type: u4
      - id: ptr_short_name
        type: u4
      - id: unk1
        type: u4
      - id: unk2
        type: u4

  thread_buckets:
    seq:
      - id: player
        type: thread_bucket
      - id: robot
        type: thread_bucket
      - id: ghost
        type: thread_bucket
      - id: static
        type: thread_bucket
      - id: mine
        type: thread_bucket
      - id: warppad
        type: thread_bucket
      - id: tracking
        type: thread_bucket
      - id: burst
        type: thread_bucket
      - id: blowup
        type: thread_bucket
      - id: turbo
        type: thread_bucket
      - id: spider
        type: thread_bucket
      - id: follower
        type: thread_bucket
      - id: start_text
        type: thread_bucket
      - id: other
        type: thread_bucket
      - id: aku_aku
        type: thread_bucket
      - id: camera
        type: thread_bucket
      - id: hud
        type: thread_bucket
      - id: pause
        type: thread_bucket

  vector2s:
    seq:
      - id: x
        type: s2
      - id: y
        type: s2

  vector3s:
    seq:
      - id: x
        type: s2
      - id: y
        type: s2
      - id: z
        type: s2