<template>
  <audio ref="alarmAudio" :src="alarmUrl" autoplay loop v-if="play"></audio>
</template>

<script>
export default {
  props: {
    play: Boolean
  },
  data() {
    return {
      alarmUrl: require('@/assets/alarm.mp3') // 放一段报警音
    };
  },
  watch: {
    play(val) {
      if (val) {
        this.$refs.alarmAudio.play();
      } else {
        this.$refs.alarmAudio.pause();
      }
    }
  }
};
</script>
